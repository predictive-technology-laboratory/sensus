// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SensusService.Probes;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using Xamarin;
using Xamarin.Geolocation;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper : IDisposable
    {
        private class ScheduledCallback
        {
            public Action<CancellationToken> Action { get; set; }
            public string Name { get; set; }
            public CancellationTokenSource Canceller { get; set; }
            public string UserNotificationMessage { get; set; }

            public ScheduledCallback(Action<CancellationToken> action, string name, CancellationTokenSource canceller, string userNotificationMessage)
            {
                Action = action;
                Name = name;
                Canceller = canceller;
                UserNotificationMessage = userNotificationMessage;
            }
        }

        #region static members
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";
        public const string SENSUS_CALLBACK_ID_KEY = "SENSUS-CALLBACK-ID";
        public const string SENSUS_CALLBACK_REPEATING_KEY = "SENSUS-CALLBACK-REPEATING";
        protected const string XAMARIN_INSIGHTS_APP_KEY = "97af5c4ab05c6a69d2945fd403ff45535f8bb9bb";
        private static SensusServiceHelper SINGLETON;
        private const string ENCRYPTION_KEY = "Making stuff private!";
        private static readonly object LOCKER = new object();
        private static readonly string SHARE_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
        private static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static readonly string SERIALIZATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");
        private static readonly JsonSerializerSettings JSON_SERIALIZATION_SETTINGS = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            };                

        private static byte[] EncryptionKeyBytes
        {
            get
            {
                byte[] encryptionKeyBytes = new byte[32];
                byte[] bytes = Encoding.Default.GetBytes(ENCRYPTION_KEY);
                Array.Copy(bytes, encryptionKeyBytes, Math.Min(bytes.Length, encryptionKeyBytes.Length));
                return encryptionKeyBytes;
            }
        }

        public static SensusServiceHelper Get()
        {
            // service helper be null for a brief period between the time when the app starts and when the service constructs the helper object.
            int triesLeft = 10;
            while (triesLeft-- > 0)
            {
                lock (LOCKER)
                    if (SINGLETON == null)
                    {
                        Console.Error.WriteLine("Waiting for service to construct helper object.");
                        Thread.Sleep(1000);
                    }
                    else
                        break;
            }

            lock (LOCKER)
                if (SINGLETON == null)
                {
                    string error = "Failed to get service helper.";

                    // don't try to access/write the logger or raise a sensus-based exception, since these will call back into the current method
                    Console.Error.WriteLine(error);

                    Exception ex = new Exception(error);

                    try { Insights.Report(ex, Xamarin.Insights.Severity.Error); }
                    catch (Exception ex2) { Console.Error.WriteLine("Failed to report exception to Xamarin Insights:  " + ex2.Message); }

                    throw ex;
                }

            return SINGLETON;
        }

        public static SensusServiceHelper Load<T>() where T : SensusServiceHelper, new()
        {
            SensusServiceHelper sensusServiceHelper = null;

            try
            {
                sensusServiceHelper = JsonConvert.DeserializeObject<T>(AesDecrypt(File.ReadAllBytes(SERIALIZATION_PATH)), JSON_SERIALIZATION_SETTINGS);
                sensusServiceHelper.Logger.Log("Deserialized service helper with " + sensusServiceHelper.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, typeof(T));  
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Failed to deserialize Sensus service helper:  " + ex.Message);
                Console.Out.WriteLine("Creating new Sensus service helper.");

                try
                {
                    sensusServiceHelper = new T();
                    sensusServiceHelper.SaveAsync();
                }
                catch (Exception ex2)
                {
                    Console.Out.WriteLine("Failed to create/save new Sensus service helper:  " + ex2.Message);
                }
            }

            return sensusServiceHelper;
        }

        #region encryption
        public static byte[] AesEncrypt(string s)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateEncryptor(encryptionKeyBytes, initialization))
                {
                    byte[] unencrypted = Encoding.Unicode.GetBytes(s);
                    return transform.TransformFinalBlock(unencrypted, 0, unencrypted.Length);
                }
            }
        }

        public static string AesDecrypt(byte[] bytes)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateDecryptor(encryptionKeyBytes, initialization))
                {
                    return Encoding.Unicode.GetString(transform.TransformFinalBlock(bytes, 0, bytes.Length));
                }
            }
        }
        #endregion
        #endregion

        private bool _stopped;
        private Logger _logger;
        private List<Protocol> _registeredProtocols;
        private List<string> _runningProtocolIds;
        private string _healthTestCallbackId;
        private int _healthTestDelayMS;
        private int _healthTestCount;
        private int _healthTestsPerProtocolReport;
        private Dictionary<string, ScheduledCallback> _idCallback;
        private MD5 _md5Hash;
        private List<PointOfInterest> _pointsOfInterest;

        private readonly object _locker = new object();

        [JsonIgnore]
        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> RegisteredProtocols
        {
            get { return _registeredProtocols; }
        }

        public List<string> RunningProtocolIds
        {
            get{ return _runningProtocolIds; }
        }                  

        [EntryIntegerUiProperty("Health Test Delay (MS):", true, 9)]
        public int HealthTestDelayMS
        {
            get { return _healthTestDelayMS; }
            set 
            {
                if (value <= 1000)
                    value = 1000;
                
                if (value != _healthTestDelayMS)
                {
                    _healthTestDelayMS = value;

                    if (_healthTestCallbackId != null)
                        _healthTestCallbackId = RescheduleRepeatingCallback(_healthTestCallbackId, _healthTestDelayMS, _healthTestDelayMS);

                    SaveAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Health Tests Per Report:", true, 10)]
        public int HealthTestsPerProtocolReport
        {
            get { return _healthTestsPerProtocolReport; }
            set
            {
                if (value != _healthTestsPerProtocolReport)
                {
                    _healthTestsPerProtocolReport = value; 
                    SaveAsync();
                }
            }
        }

        [ListUiProperty("Logging Level:", true, 11, new object[] { LoggingLevel.Off, LoggingLevel.Normal, LoggingLevel.Verbose, LoggingLevel.Debug })]
        public LoggingLevel LoggingLevel
        {
            get { return _logger.Level; }
            set 
            {
                if (value != _logger.Level)
                {
                    _logger.Level = value; 
                    SaveAsync();
                }
            }
        }

        public List<PointOfInterest> PointsOfInterest
        {
            get { return _pointsOfInterest; }
        }

        #region platform-specific properties
        [JsonIgnore]
        public abstract bool IsCharging { get; }

        [JsonIgnore]
        public abstract bool WiFiConnected { get; }

        [JsonIgnore]
        public abstract string DeviceId { get; }       

        [JsonIgnore]
        public abstract string OperatingSystem { get; }

        protected abstract Geolocator Geolocator { get; }
        #endregion

        protected SensusServiceHelper()
        {
            lock (LOCKER)
                if (SINGLETON != null)
                    SINGLETON.Dispose();

            _stopped = true;
            _registeredProtocols = new List<Protocol>();
            _runningProtocolIds = new List<string>();
            _healthTestCallbackId = null;
            _healthTestDelayMS = 60000;
            _healthTestCount = 0;
            _healthTestsPerProtocolReport = 5;
            _idCallback = new Dictionary<string, ScheduledCallback>();
            _md5Hash = MD5.Create();
            _pointsOfInterest = new List<PointOfInterest>();

            if (!Directory.Exists(SHARE_DIRECTORY))
                Directory.CreateDirectory(SHARE_DIRECTORY); 

            #if DEBUG
            LoggingLevel loggingLevel = LoggingLevel.Debug;
            #else
            LoggingLevel loggingLevel = LoggingLevel.Normal;
            #endif

            _logger = new Logger(LOG_PATH, loggingLevel, Console.Error);
            _logger.Log("Log file started at \"" + LOG_PATH + "\".", LoggingLevel.Normal, GetType());

            GpsReceiver.Get().Initialize(Geolocator);  // initialize GPS receiver with platform-specific geolocator

            if (Insights.IsInitialized)
                _logger.Log("Xamarin Insights is already initialized.", LoggingLevel.Normal, GetType());
            else
            {
                try
                {
                    _logger.Log("Initializing Xamarin Insights.", LoggingLevel.Normal, GetType());
                    InitializeXamarinInsights();
                }
                catch (Exception ex)
                {
                    _logger.Log("Failed to initialize Xamarin insights:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            lock (LOCKER)
                SINGLETON = this;
        }

        public string GetMd5Hash(string s)
        {
            if (s == null)
                return null;
            
            StringBuilder hashBuilder = new StringBuilder();
            foreach (byte b in _md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s)))
                hashBuilder.Append(b.ToString("x"));

            return hashBuilder.ToString();
        }           

        #region platform-specific methods
        protected abstract void InitializeXamarinInsights();

        public abstract bool Use(Probe probe);

        protected abstract void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, string userNotificationMessage);

        protected abstract void ScheduleOneTimeCallback(string callbackId, int delayMS, string userNotificationMessage);

        protected abstract void UnscheduleCallback(string callbackId, bool repeating);

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject);

        public abstract void TextToSpeechAsync(string text, Action callback);

        public abstract void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback);

        public abstract void IssueNotificationAsync(string message, string id);

        public abstract void FlashNotificationAsync(string message, Action callback);

        public abstract void KeepDeviceAwake();

        public abstract void LetDeviceSleep();

        public abstract void UpdateApplicationStatus(string status);
        #endregion

        #region add/remove running protocol ids
        public void AddRunningProtocolId(string id)
        {
            lock (_locker)
            {
                if (!_runningProtocolIds.Contains(id))
                {
                    _runningProtocolIds.Add(id);
                    SaveAsync();

                    SensusServiceHelper.Get().UpdateApplicationStatus(_runningProtocolIds.Count + " protocol" + (_runningProtocolIds.Count == 1 ? " is " : "s are") + " running");
                }

                if (_healthTestCallbackId == null)
                    _healthTestCallbackId = ScheduleRepeatingCallback(TestHealth, "Test Health", _healthTestDelayMS, _healthTestDelayMS);
            }
        }

        public void RemoveRunningProtocolId(string id)
        {
            lock (_locker)
            {
                if (_runningProtocolIds.Remove(id))
                {
                    SaveAsync();

                    SensusServiceHelper.Get().UpdateApplicationStatus(_runningProtocolIds.Count + " protocol" + (_runningProtocolIds.Count == 1 ? " is " : "s are") + " running");
                }

                if (_runningProtocolIds.Count == 0)
                {
                    UnscheduleRepeatingCallback(_healthTestCallbackId);
                    _healthTestCallbackId = null;
                }
            }
        }

        public bool ProtocolShouldBeRunning(Protocol protocol)
        {
            return _runningProtocolIds.Contains(protocol.Id);
        }
        #endregion

        public void SaveAsync()
        {
            new Thread(() =>
                {
                    lock (_locker)
                    {
                        try
                        {
                            using (FileStream file = new FileStream(SERIALIZATION_PATH, FileMode.Create, FileAccess.Write))
                            {
                                byte[] encryptedBytes = AesEncrypt(JsonConvert.SerializeObject(this, JSON_SERIALIZATION_SETTINGS));
                                file.Write(encryptedBytes, 0, encryptedBytes.Length);
                                file.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log("Failed to serialize Sensus service helper:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }

                }).Start();
        }

        public void StartAsync(Action callback)
        {
            new Thread(() =>
                {
                    Start();

                    if(callback != null)
                        callback();

                }).Start();
        }

        /// <summary>
        /// Starts platform-independent service functionality, including protocols that should be running. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void Start()
        {
            lock (_locker)
            {
                if (_stopped)
                    _stopped = false;
                else
                    return;

                foreach (Protocol protocol in _registeredProtocols)
                    if (!protocol.Running && _runningProtocolIds.Contains(protocol.Id))
                        protocol.Start();
            }
        }        

        public void RegisterProtocol(Protocol protocol)
        {
            lock (_locker)
                if (!_stopped && !_registeredProtocols.Contains(protocol))
                {
                    _registeredProtocols.Add(protocol);
                    SaveAsync();
                }
        }

        #region callback scheduling
        public string ScheduleRepeatingCallback(Action<CancellationToken> callback, string name, int initialDelayMS, int repeatDelayMS)
        {
            return ScheduleRepeatingCallback(callback, name, initialDelayMS, repeatDelayMS, null);
        }

        public string ScheduleRepeatingCallback(Action<CancellationToken> callback, string name, int initialDelayMS, int repeatDelayMS, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback, name, userNotificationMessage);
                ScheduleRepeatingCallback(callbackId, initialDelayMS, repeatDelayMS, userNotificationMessage);
                return callbackId;
            }
        }

        public string ScheduleOneTimeCallback(Action<CancellationToken> callback, string name, int delay)
        {
            return ScheduleOneTimeCallback(callback, name, delay, null);
        }

        public string ScheduleOneTimeCallback(Action<CancellationToken> callback, string name, int delay, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback, name, userNotificationMessage);
                ScheduleOneTimeCallback(callbackId, delay, userNotificationMessage);
                return callbackId;
            }
        }

        private string AddCallback(Action<CancellationToken> callback, string name, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = Guid.NewGuid().ToString();
                _idCallback.Add(callbackId, new ScheduledCallback(callback, name, null, userNotificationMessage));
                return callbackId;
            }
        }

        public bool CallbackIsScheduled(string callbackId)
        {
            lock(_idCallback)
                return _idCallback.ContainsKey(callbackId);
        }

        public string RescheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS)
        {
            lock (_idCallback)
            {
                ScheduledCallback scheduledCallback;
                if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
                {
                    UnscheduleRepeatingCallback(callbackId);
                    return ScheduleRepeatingCallback(scheduledCallback.Action, scheduledCallback.Name, initialDelayMS, repeatDelayMS, scheduledCallback.UserNotificationMessage);
                }
                else
                    return null;
            }
        }

        public void RaiseCallbackAsync(string callbackId, bool repeating, bool notifyUser)
        {
            RaiseCallbackAsync(callbackId, repeating, notifyUser, null);
        }    

        public void RaiseCallbackAsync(string callbackId, bool repeating, bool notifyUser, Action callback)
        {
            lock (_idCallback)
            {
                // do we have callback information for the passed callbackId? we might not, in the case where the callback is canceled by the user and the system fires it subsequently.
                ScheduledCallback scheduledCallback;
                if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
                {
                    KeepDeviceAwake();  // not all OSs support this (e.g., iOS), but call it anyway

                    new Thread(() =>
                        {
                            // callbacks cannot be raised concurrently -- drop the current callback if it is already in progress.
                            if (Monitor.TryEnter(scheduledCallback.Action))
                            {
                                // initialize a new cancellation token source for this call, since they cannot be reset
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                                // set cancellation token source in collection, so that someone can call CancelRaisedCallback -- lock the containing collection since we lock it within CancelRaisedCallback
                                lock (_idCallback)
                                    scheduledCallback.Canceller = cancellationTokenSource;

                                try
                                {
                                    _logger.Log("Raising callback \"" + scheduledCallback.Name + "\" (" + callbackId + ").", LoggingLevel.Debug, GetType());

                                    if(notifyUser)
                                        IssueNotificationAsync(scheduledCallback.UserNotificationMessage, callbackId);

                                    scheduledCallback.Action(cancellationTokenSource.Token);
                                }
                                catch (Exception ex)
                                {
                                    _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                                }
                                finally
                                {
                                    Monitor.Exit(scheduledCallback.Action);

                                    // reset cancellation token to null since there is nothing to cancel -- lock the containing collection since we lock it within CancelRaisedCallback
                                    lock (_idCallback)
                                        scheduledCallback.Canceller = null;
                                }
                            }
                            else
                                _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") was already running. Not running again.", LoggingLevel.Debug, GetType());

                            if (!repeating)
                                lock (_idCallback)
                                    _idCallback.Remove(callbackId);

                            LetDeviceSleep();

                            if (callback != null)
                                callback();

                        }).Start();                           
                }
            }
        }

        public void CancelRaisedCallback(string callbackId)
        {
            lock (_idCallback)
            {
                ScheduledCallback scheduledCallback;
                if (_idCallback.TryGetValue(callbackId, out scheduledCallback) && scheduledCallback.Canceller != null)  // the cancellation source will be null if the callback is not currently being raised
                    scheduledCallback.Canceller.Cancel();
            }
        }

        public void UnscheduleOneTimeCallback(string callbackId)
        {
            if (callbackId != null)
                lock (_idCallback)
                {
                    _idCallback.Remove(callbackId);
                    UnscheduleCallback(callbackId, false);
                }
        }

        public void UnscheduleRepeatingCallback(string callbackId)
        {                                      
            if (callbackId != null)
                lock (_idCallback)
                {
                    _idCallback.Remove(callbackId);
                    UnscheduleCallback(callbackId, true);
                }
        }
        #endregion

        public void TextToSpeechAsync(string text)
        {
            TextToSpeechAsync(text, () =>
                {
                });                        
        }

        public void FlashNotificationAsync(string message)
        {
            FlashNotificationAsync(message, () =>
                {
                });
        }

        public void TestHealth(CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                if (_stopped)
                    return;

                _logger.Log("Sensus health test is running (test " + ++_healthTestCount + ")", LoggingLevel.Normal, GetType());

                foreach (Protocol protocol in _registeredProtocols)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    if (_runningProtocolIds.Contains(protocol.Id))
                    {
                        protocol.TestHealth();

                        if (_healthTestCount % _healthTestsPerProtocolReport == 0)
                            protocol.StoreMostRecentProtocolReport();
                    }
                }
            }
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (_locker)
            {
                protocol.Stop();
                _registeredProtocols.Remove(protocol);
                SaveAsync();
            }
        }

        /// <summary>
        /// Stops the service helper, but leaves it in a state in which subsequent calls to Start will succeed. This happens, for example, when the service is stopped and then 
        /// restarted without being destroyed.
        /// </summary>
        public void StopAsync()
        {
            new Thread(() =>
                {
                    Stop();

                }).Start();
        }

        public virtual void Stop()
        {
            lock (_locker)
            {
                if (_stopped)
                    return;

                _logger.Log("Stopping Sensus service.", LoggingLevel.Normal, GetType());

                foreach (Protocol protocol in _registeredProtocols)
                    protocol.Stop();

                _stopped = true;
            }
        }

        public string GetSharePath(string extension)
        {
            lock (_locker)
            {
                int fileNum = 0;
                string path = null;
                while (path == null || File.Exists(path))
                    path = Path.Combine(SHARE_DIRECTORY, fileNum++ + (string.IsNullOrWhiteSpace(extension) ? "" : "." + extension.Trim('.')));

                return path;
            }
        }

        public virtual void Destroy()
        {
            Dispose();           
        }           

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Failed to stop service helper:  " + ex.Message);
            }

            try
            {
                _logger.Close();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Failed to close logger:  " + ex.Message);
            }

            _md5Hash.Dispose();
            _md5Hash = null;

            SINGLETON = null;
        }
    }
}

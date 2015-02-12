#region copyright
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
#endregion

using Newtonsoft.Json;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xamarin;
using Xamarin.Geolocation;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper : IDisposable
    {
        #region static members
        private static SensusServiceHelper _singleton;
        private static object _staticLockObject = new object();
        private static readonly string _shareDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
        private static readonly string _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static readonly string _logTag = "SERVICE-HELPER";
        private static readonly string _serializationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");
        private static readonly JsonSerializerSettings _serializationSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            };

        public static SensusServiceHelper Get()
        {
            // service helper be null for a brief period between the time when the app starts and when the service constructs the helper object.
            int triesLeft = 10;
            while (triesLeft-- > 0)
            {
                lock (_staticLockObject)
                    if (_singleton == null)
                    {
                        Console.Error.WriteLine("Waiting for service to construct helper object.");
                        Thread.Sleep(1000);
                    }
                    else
                        break;
            }

            lock (_staticLockObject)
                if (_singleton == null)
                {
                    string error = "Failed to get service helper.";

                    // don't try to access/write the logger or raise a sensus-based exception, since these will call back into the current method
                    Console.Error.WriteLine(error);

                    Exception ex = new Exception(error);

                    try { Insights.Report(ex, ReportSeverity.Error); }
                    catch (Exception ex2) { Console.Error.WriteLine("Failed to report exception to Xamarin Insights:  " + ex2.Message); }

                    throw ex;
                }

            return _singleton;
        }

        public static SensusServiceHelper Load<T>(Geolocator geolocator) where T : SensusServiceHelper
        {
            SensusServiceHelper sensusServiceHelper = null;

            try
            {
                sensusServiceHelper = JsonConvert.DeserializeObject<T>(File.ReadAllText(_serializationPath), _serializationSettings);
                sensusServiceHelper.Initialize(geolocator);
                sensusServiceHelper.Logger.Log("Deserialized service helper with " + sensusServiceHelper.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, _logTag);  
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Failed to deserialize Sensus service helper:  " + ex.Message);
                sensusServiceHelper = null;
            }

            return sensusServiceHelper;
        }
        #endregion

        protected const string XAMARIN_INSIGHTS_APP_KEY = "97af5c4ab05c6a69d2945fd403ff45535f8bb9bb";

        /// <summary>
        /// Raised when the service helper has stopped.
        /// </summary>
        public event EventHandler Stopped;       

        private bool _stopped;
        private Logger _logger;
        private List<Protocol> _registeredProtocols;
        private List<string> _runningProtocolIds;
        private int _healthTestCallbackId;
        private int _healthTestDelayMS;
        private int _healthTestCount;
        private int _healthTestsPerProtocolReport;
        private Dictionary<int, Action> _idCallback;

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

        [JsonIgnore]
        public abstract bool IsCharging { get; }

        [JsonIgnore]
        public abstract bool WiFiConnected { get; }

        [JsonIgnore]
        public abstract string DeviceId { get; }

        [JsonIgnore]
        public abstract bool DeviceHasMicrophone { get; }

        [EntryIntegerUiProperty("Health Test Delay (MS):", true, 9)]
        public int HealthTestDelayMS
        {
            get { return _healthTestDelayMS; }
            set 
            {
                if (value != _healthTestDelayMS)
                {
                    _healthTestDelayMS = value;
                    UpdateCallback(_healthTestCallbackId, _healthTestDelayMS, _healthTestDelayMS);
                    Save();
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
                    Save();
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
                    Save();
                }
            }
        }

        protected SensusServiceHelper()
        {
            lock (_staticLockObject)
                if (_singleton != null)
                    _singleton.Dispose();

            _stopped = true;
            _registeredProtocols = new List<Protocol>();
            _runningProtocolIds = new List<string>();
            _healthTestCallbackId = -1;
            _healthTestDelayMS = 1000 * 60;
            _healthTestCount = 0;
            _healthTestsPerProtocolReport = 5;
            _idCallback = new Dictionary<int, Action>();

            if (!Directory.Exists(_shareDirectory))
                Directory.CreateDirectory(_shareDirectory); 

            #region logger
            #if DEBUG
            _logger = new Logger(_logPath, LoggingLevel.Debug, Console.Error);
            #else
            _logger = new Logger(_logPath, LoggingLevel.Normal, Console.Error);
            #endif

            _logger.Log("Log file started at \"" + _logPath + "\".", LoggingLevel.Normal, _logTag);
            #endregion

            lock (_staticLockObject)
                _singleton = this;
        }

        public void Initialize(Geolocator geolocator)
        {
            GpsReceiver.Get().Initialize(geolocator);

            try { InitializeXamarinInsights(); }
            catch (Exception ex) { _logger.Log("Failed to initialize Xamarin insights:  " + ex.Message, LoggingLevel.Normal); }                              
        }

        #region platform-specific methods
        protected abstract void InitializeXamarinInsights();

        protected abstract void ScheduleCallbackInternal(int callbackId, int initialDelayMS, int subsequentDelayMS);

        protected abstract void CancelCallbackInternal(int callbackId);

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject);

        public void TextToSpeechAsync(string text)
        {
            TextToSpeechAsync(text, () =>
                {
                });                        
        }

        public abstract void TextToSpeechAsync(string text, Action callback);

        public abstract void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback);

        public void FlashNotificationAsync(string message)
        {
            FlashNotificationAsync(message, () =>
                {
                });
        }

        public abstract void FlashNotificationAsync(string message, Action callback);
        #endregion

        #region add/remove running protocol ids
        public void AddRunningProtocolId(string id)
        {
            lock (this)
            {
                if (!_runningProtocolIds.Contains(id))
                {
                    _runningProtocolIds.Add(id);
                    Save();
                }

                if (_healthTestCallbackId == -1)
                    _healthTestCallbackId = ScheduleCallback(TestHealth, _healthTestDelayMS, _healthTestDelayMS);
            }
        }

        public void RemoveRunningProtocolId(string id)
        {
            lock (this)
            {
                if (_runningProtocolIds.Remove(id))
                    Save();

                if (_runningProtocolIds.Count == 0)
                    CancelCallback(_healthTestCallbackId);
            }
        }

        public bool ProtocolShouldBeRunning(Protocol protocol)
        {
            return _runningProtocolIds.Contains(protocol.Id);
        }
        #endregion

        public void Save()
        {
            lock (this)
            {
                try
                {
                    File.WriteAllText(_serializationPath, JsonConvert.SerializeObject(this, _serializationSettings));
                }
                catch (Exception ex)
                {
                    _logger.Log("Failed to serialize Sensus service helper:  " + ex.Message, LoggingLevel.Normal);
                }
            }
        }

        /// <summary>
        /// Starts platform-independent service functionality, including protocols that should be running. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void Start()
        {
            lock (this)
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
            lock (this)
                if (!_stopped && !_registeredProtocols.Contains(protocol))
                {
                    _registeredProtocols.Add(protocol);
                    Save();
                }
        }

        public int ScheduleCallback(Action callback, int initialDelayMS, int subsequentDelayMS)
        {
            lock (_idCallback)
            {
                int callbackId = 0;
                while (_idCallback.ContainsKey(callbackId))
                    ++callbackId;

                _idCallback.Add(callbackId, callback);
                ScheduleCallbackInternal(callbackId, initialDelayMS, subsequentDelayMS);

                return callbackId;
            }
        }

        public void UpdateCallback(int callbackId, int initialDelayMS, int subsequentDelayMS)
        {
            lock (_idCallback)
                if (_idCallback.ContainsKey(callbackId))
                    ScheduleCallbackInternal(callbackId, initialDelayMS, subsequentDelayMS);
        }

        public void CancelCallback(int callbackId)
        {
            lock (_idCallback)
            {
                CancelCallbackInternal(callbackId);
                if (_idCallback.ContainsKey(callbackId))
                    _idCallback.Remove(callbackId);
            }
        }

        public void RaiseCallbackAsync(int callbackId)
        {
            lock (_idCallback)
            {
                Action callback;
                if (_idCallback.TryGetValue(callbackId, out callback))
                    new Thread(() =>
                        {
                            try
                            {
                                _logger.Log("Raising callback " + callbackId, LoggingLevel.Verbose);
                                callback();
                            }
                            catch (Exception ex)
                            {
                                _logger.Log("Callback failed:  " + ex.Message, LoggingLevel.Normal);
                            }

                        }).Start();
            }
        }

        public void TestHealth()
        {
            lock (this)
            {
                if (_stopped)
                    return;

                _logger.Log("Sensus health test is running (test " + ++_healthTestCount + ")", LoggingLevel.Normal, _logTag);

                foreach (Protocol protocol in _registeredProtocols)
                    if (_runningProtocolIds.Contains(protocol.Id))
                    {
                        protocol.TestHealth();

                        if (_healthTestCount % _healthTestsPerProtocolReport == 0)
                            protocol.StoreMostRecentProtocolReport();
                    }
            }
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (this)
            {
                protocol.Stop();
                _registeredProtocols.Remove(protocol);
                Save();
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

        public void Stop()
        {
            lock (this)
            {
                if (_stopped)
                    return;

                _logger.Log("Stopping Sensus service.", LoggingLevel.Normal, _logTag);

                foreach (Protocol protocol in _registeredProtocols)
                    protocol.Stop();

                _stopped = true;
            }

            // let others (e.g., platform-specific services and applications) know that we've stopped
            if (Stopped != null)
                Stopped(null, null);
        }

        public string GetSharePath(string extension)
        {
            lock (this)
            {
                int fileNum = 0;
                string path = null;
                while (path == null || File.Exists(path))
                    path = Path.Combine(_shareDirectory, fileNum++ + (string.IsNullOrWhiteSpace(extension) ? "" : "." + extension.Trim('.')));

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
            catch(Exception ex)
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

            _singleton = null;
        }
    }
}

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
using System.Collections.ObjectModel;
using SensusUI;
using SensusUI.Inputs;
using Xamarin.Forms;
using SensusService.Exceptions;
using ZXing.Mobile;
using ZXing;
using XLabs.Platform.Device;
using System.Collections;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper : IDisposable
    {
        /// <summary>
        /// Encapsulates information needed to run a scheduled callback.
        /// </summary>
        private class ScheduledCallback
        {
            /// <summary>
            /// Action to invoke.
            /// </summary>
            /// <value>The action.</value>
            public Action<string, CancellationToken> Action { get; set; }

            /// <summary>
            /// Name of callback.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }

            /// <summary>
            /// Source of cancellation tokens when Action is invoked.
            /// </summary>
            /// <value>The canceller.</value>
            public CancellationTokenSource Canceller { get; set; }

            /// <summary>
            /// Notification message that should be displayed to the user when the callback is invoked.
            /// </summary>
            /// <value>The user notification message.</value>
            public string UserNotificationMessage { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SensusService.SensusServiceHelper+ScheduledCallback"/> class.
            /// </summary>
            /// <param name="action">Action.</param>
            /// <param name="name">Name.</param>
            /// <param name="canceller">Canceller.</param>
            /// <param name="userNotificationMessage">User notification message.</param>
            public ScheduledCallback(Action<string, CancellationToken> action, string name, CancellationTokenSource canceller, string userNotificationMessage)
            {
                Action = action;
                Name = name;
                Canceller = canceller;
                UserNotificationMessage = userNotificationMessage;
            }
        }

        #region static members

        private static SensusServiceHelper SINGLETON;
        private static readonly object PROMPT_FOR_INPUTS_LOCKER = new object();
        private static bool PROMPT_FOR_INPUTS_RUNNING = false;
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";
        public const string SENSUS_CALLBACK_ID_KEY = "SENSUS-CALLBACK-ID";
        public const string SENSUS_CALLBACK_REPEATING_KEY = "SENSUS-CALLBACK-REPEATING";
        public const int PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS = 60;
        protected const string XAMARIN_INSIGHTS_APP_KEY = "";
        private const string ENCRYPTION_KEY = "";
        private static readonly string SHARE_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
        private static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static readonly string SERIALIZATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");

        public static bool PromptForInputsRunning
        {
            get { return PROMPT_FOR_INPUTS_RUNNING; }
        }

        #if DEBUG || UNIT_TESTING
        public const int HEALTH_TEST_DELAY_MS = 30000;
        #elif RELEASE
        public const int HEALTH_TEST_DELAY_MS = 300000;
        #endif

        public static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,

            #region need the following in order to deserialize protocols between OSs, whose objects contain different members (e.g., iOS service helper has ActivationId, which Android does not)
            Error = (o, e) =>
            {
                SensusServiceHelper.Get().Logger.Log("Failed to deserialize some part of the JSON:  " + e.ErrorContext.Error.ToString(), LoggingLevel.Normal, typeof(Protocol));
                e.ErrorContext.Handled = true;
            },

            MissingMemberHandling = MissingMemberHandling.Ignore  
            #endregion
        };

        private static byte[] EncryptionKeyBytes
        {
            get
            {
                byte[] encryptionKeyBytes = new byte[32];
                byte[] bytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
                Array.Copy(bytes, encryptionKeyBytes, Math.Min(bytes.Length, encryptionKeyBytes.Length));
                return encryptionKeyBytes;
            }
        }

        /// <summary>
        /// Initializes the sensus service helper. Must be called when app first starts, from the main / UI thread.
        /// </summary>
        /// <param name="createNew">Function for creating a new service helper, if one is needed.</param>
        public static void Initialize(Func<SensusServiceHelper> createNew)
        {
            if (SINGLETON != null)
            {
                SINGLETON.Logger.Log("Serivce helper already initialized. Nothing to do.", LoggingLevel.Normal, SINGLETON.GetType());
                return;
            }
            
            try
            {
                SINGLETON = JsonConvert.DeserializeObject<SensusServiceHelper>(Decrypt(ReadAllBytes(SERIALIZATION_PATH)), JSON_SERIALIZER_SETTINGS);
                SINGLETON.Logger.Log("Deserialized service helper with " + SINGLETON.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, SINGLETON.GetType());
            }
            catch (Exception deserializeException)
            {
                Console.Error.WriteLine("Failed to deserialize Sensus service helper:  " + deserializeException.Message);

                try
                {
                    Console.Error.WriteLine("Creating new Sensus service helper.");
                    SINGLETON = createNew();
                }
                catch (Exception singletonCreationException)
                {
                    #region crash app and report to insights
                    string error = "Failed to construct service helper:  " + singletonCreationException.Message + System.Environment.NewLine + singletonCreationException.StackTrace;
                    Console.Error.WriteLine(error);
                    Exception exceptionToReport = new Exception(error);

                    try
                    {
                        Insights.Report(exceptionToReport, Xamarin.Insights.Severity.Error);
                    }
                    catch (Exception insightsReportException)
                    {
                        Console.Error.WriteLine("Failed to report exception to Xamarin Insights:  " + insightsReportException.Message);
                    }

                    throw exceptionToReport;
                    #endregion
                }

                #region save newly created helper
                try
                {
                    SINGLETON.SaveAsync();
                }
                catch (Exception singletonSaveException)
                {
                    Console.Error.WriteLine("Failed to save new Sensus service helper:  " + singletonSaveException.Message);
                }
                #endregion
            }  
        }

        public static SensusServiceHelper Get()
        {
            return SINGLETON;
        }

        /// <summary>
        /// Reads all bytes from a file. There's a File.ReadAllBytes method in Android / iOS, but not in WinPhone.
        /// </summary>
        /// <returns>The bytes.</returns>
        /// <param name="path">Path.</param>
        public static byte[] ReadAllBytes(string path)
        {
            byte[] bytes = null;

            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[file.Length];
                int blockSize = 1024;
                byte[] block = new byte[blockSize];
                int bytesRead;
                int totalBytesRead = 0;
                while ((bytesRead = file.Read(block, 0, block.Length)) > 0)
                {
                    Array.Copy(block, 0, bytes, totalBytesRead, bytesRead);
                    totalBytesRead += bytesRead;
                }

                file.Close();
            }

            return bytes;
        }

        #region encryption

        public static byte[] Encrypt(string unencryptedString)
        {
            #if (__ANDROID__ || __IOS__)
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateEncryptor(encryptionKeyBytes, initialization))
                {
                    byte[] unencrypted = Encoding.Unicode.GetBytes(unencryptedString);
                    return transform.TransformFinalBlock(unencrypted, 0, unencrypted.Length);
                }
            }
            #elif WINDOWS_PHONE
            return ProtectedData.Protect(Encoding.Unicode.GetBytes(unencryptedString), EncryptionKeyBytes);
            #else
            #error "Unrecognized platform."
            #endif
        }

        public static string Decrypt(byte[] encryptedBytes)
        {
            #if __ANDROID__ || __IOS__
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateDecryptor(encryptionKeyBytes, initialization))
                {
                    return Encoding.Unicode.GetString(transform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
                }
            }
            #elif WINDOWS_PHONE
            byte[] unencryptedBytes = ProtectedData.Unprotect(encryptedBytes, EncryptionKeyBytes);
            return Encoding.Unicode.GetString(unencryptedBytes, 0, unencryptedBytes.Length);
            #else
            #error "Unrecognized platform."
            #endif
        }

        #endregion

        #endregion

        private bool _stopped;
        private Logger _logger;
        private ObservableCollection<Protocol> _registeredProtocols;
        private List<string> _runningProtocolIds;
        private string _healthTestCallbackId;
        private Dictionary<string, ScheduledCallback> _idCallback;
        private SHA256Managed _hasher;
        private List<PointOfInterest> _pointsOfInterest;
        private ZXing.Mobile.MobileBarcodeScanner _barcodeScanner;
        private ZXing.Mobile.BarcodeWriter _barcodeWriter;

        private readonly object _locker = new object();

        [JsonIgnore]
        public Logger Logger
        {
            get { return _logger; }
        }

        public ObservableCollection<Protocol> RegisteredProtocols
        {
            get { return _registeredProtocols; }
        }

        public List<string> RunningProtocolIds
        {
            get{ return _runningProtocolIds; }
        }

        public List<PointOfInterest> PointsOfInterest
        {
            get { return _pointsOfInterest; }
        }

        [JsonIgnore]
        public ZXing.Mobile.MobileBarcodeScanner BarcodeScanner
        {
            get
            {
                return _barcodeScanner; 
            }
            set
            {
                _barcodeScanner = value;
            }
        }

        [JsonIgnore]
        public ZXing.Mobile.BarcodeWriter BarcodeWriter
        {
            get
            {
                return _barcodeWriter; 
            }
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
            if (SINGLETON != null)
                throw new SensusException("Attempted to construct new service helper when singleton already existed.");

            _stopped = true;
            _registeredProtocols = new ObservableCollection<Protocol>();
            _runningProtocolIds = new List<string>();
            _healthTestCallbackId = null;
            _idCallback = new Dictionary<string, ScheduledCallback>();
            _hasher = new SHA256Managed();
            _pointsOfInterest = new List<PointOfInterest>();

            // ensure that the entire QR code is always visible by using 90% the minimum screen dimension as the QR code size.
            #if __ANDROID__
            int qrCodeSize = (int)(0.9 * Math.Min(XLabs.Platform.Device.Display.Metrics.WidthPixels, XLabs.Platform.Device.Display.Metrics.HeightPixels));
            #elif __IOS__
            int qrCodeSize = (int)(0.9 * Math.Min(AppleDevice.CurrentDevice.Display.Height, AppleDevice.CurrentDevice.Display.Width));
            #else
            #error "Unrecognized platform"
            #endif

            _barcodeWriter = new ZXing.Mobile.BarcodeWriter
            { 
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = qrCodeSize,
                    Width = qrCodeSize
                }
            };

            if (!Directory.Exists(SHARE_DIRECTORY))
                Directory.CreateDirectory(SHARE_DIRECTORY); 

            #if DEBUG || UNIT_TESTING
            LoggingLevel loggingLevel = LoggingLevel.Debug;
            #elif RELEASE
            LoggingLevel loggingLevel = LoggingLevel.Normal;
            #else
            #error "Unrecognized configuration."
            #endif

            _logger = new Logger(LOG_PATH, loggingLevel, Console.Error);
            _logger.Log("Log file started at \"" + LOG_PATH + "\".", LoggingLevel.Normal, GetType());

            GpsReceiver.Get().Initialize(Geolocator);  // initialize GPS receiver with platform-specific geolocator

            if (Insights.IsInitialized)
                _logger.Log("Xamarin Insights is already initialized.", LoggingLevel.Normal, GetType());
            else if (string.IsNullOrWhiteSpace(XAMARIN_INSIGHTS_APP_KEY))
                _logger.Log("Xamarin Insights API key is empty. Not initializing.", LoggingLevel.Normal, GetType());  // xamarin allows to initialize with a null key, which fails with exception but results in IsInitialized being true. prevent that here.
            else
            {
                try
                {
                    _logger.Log("Initializing Xamarin Insights.", LoggingLevel.Normal, GetType());

                    // wait for startup crash to be logged -- https://insights.xamarin.com/docs
                    Insights.HasPendingCrashReport += (sender, isStartupCrash) =>
                    {
                        if (isStartupCrash)
                            Insights.PurgePendingCrashReports().Wait();
                    };

                    InitializeXamarinInsights();  
                }
                catch (Exception ex)
                {
                    _logger.Log("Failed to initialize Xamarin insights:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        public string GetHash(string s)
        {
            if (s == null)
                return null;
            
            StringBuilder hashBuilder = new StringBuilder();
            foreach (byte b in _hasher.ComputeHash(Encoding.UTF8.GetBytes(s)))
                hashBuilder.Append(b.ToString("x"));

            return hashBuilder.ToString();
        }

        #region platform-specific methods. this functionality cannot be implemented in a cross-platform way. it must be done separately for each platform.

        protected abstract void InitializeXamarinInsights();

        protected abstract void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, string userNotificationMessage);

        protected abstract void ScheduleOneTimeCallback(string callbackId, int delayMS, string userNotificationMessage);

        protected abstract void UnscheduleCallback(string callbackId, bool repeating);

        protected abstract void ProtectedFlashNotificationAsync(string message, Action callback);

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject, string mimeType);

        public abstract void SendEmailAsync(string toAddress, string subject, string message);

        public abstract void TextToSpeechAsync(string text, Action callback);

        public abstract void RunVoicePromptAsync(string prompt, Action<string> callback);

        public abstract void IssueNotificationAsync(string message, string id);

        public abstract void KeepDeviceAwake();

        public abstract void LetDeviceSleep();

        public abstract void BringToForeground();

        /// <summary>
        /// The user can enable all probes at once. When this is done, it doesn't make sense to enable, e.g., the
        /// listening location probe as well as the polling location probe. This method allows the platforms to
        /// decide which probes to enable when enabling all probes.
        /// </summary>
        /// <returns><c>true</c>, if probe should be enabled, <c>false</c> otherwise.</returns>
        /// <param name="probe">Probe.</param>
        public abstract bool EnableProbeWhenEnablingAll(Probe probe);

        public abstract ImageSource GetQrCodeImageSource(string contents);

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
                }

                if (_healthTestCallbackId == null)
                    _healthTestCallbackId = ScheduleRepeatingCallback(TestHealth, "Test Health", HEALTH_TEST_DELAY_MS, HEALTH_TEST_DELAY_MS);
            }
        }

        public void RemoveRunningProtocolId(string id)
        {
            lock (_locker)
            {
                if (_runningProtocolIds.Remove(id))
                    SaveAsync();

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
                                byte[] encryptedBytes = Encrypt(JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS));
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

                    if (callback != null)
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

        public string ScheduleRepeatingCallback(Action<string, CancellationToken> callback, string name, int initialDelayMS, int repeatDelayMS)
        {
            return ScheduleRepeatingCallback(callback, name, initialDelayMS, repeatDelayMS, null);
        }

        public string ScheduleRepeatingCallback(Action<string, CancellationToken> callback, string name, int initialDelayMS, int repeatDelayMS, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback, name, userNotificationMessage);
                ScheduleRepeatingCallback(callbackId, initialDelayMS, repeatDelayMS, userNotificationMessage);
                return callbackId;
            }
        }

        public string ScheduleOneTimeCallback(Action<string, CancellationToken> callback, string name, int delayMS)
        {
            return ScheduleOneTimeCallback(callback, name, delayMS, null);
        }

        public string ScheduleOneTimeCallback(Action<string, CancellationToken> callback, string name, int delayMS, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback, name, userNotificationMessage);
                ScheduleOneTimeCallback(callbackId, delayMS, userNotificationMessage);
                return callbackId;
            }
        }

        private string AddCallback(Action<string, CancellationToken> callback, string name, string userNotificationMessage)
        {
            lock (_idCallback)
            {
                string callbackId = Guid.NewGuid().ToString();
                _idCallback.Add(callbackId, new ScheduledCallback(callback, name, new CancellationTokenSource(), userNotificationMessage));
                return callbackId;
            }
        }

        public bool CallbackIsScheduled(string callbackId)
        {
            lock (_idCallback)
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
            KeepDeviceAwake();  // call this before we start up the new thread, just in case the system decides to sleep before the thread is started.

            new Thread(() =>
                {
                    try
                    {
                        ScheduledCallback scheduledCallback;

                        lock (_idCallback)
                        {
                            // do we have callback information for the passed callbackId? we might not, in the case where the callback is canceled by the user and the system fires it subsequently.
                            if (!_idCallback.TryGetValue(callbackId, out scheduledCallback))
                            {
                                if (callback != null)
                                    callback();

                                return;
                            }
                        }

                        // callback actions cannot be raised concurrently -- drop the current callback if it is already in progress
                        if (Monitor.TryEnter(scheduledCallback.Action))
                        {
                            try
                            {
                                if (scheduledCallback.Canceller.IsCancellationRequested)
                                    _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") was cancelled before it was started.", LoggingLevel.Normal, GetType());
                                else
                                {
                                    _logger.Log("Raising callback \"" + scheduledCallback.Name + "\" (" + callbackId + ").", LoggingLevel.Normal, GetType());

                                    if (notifyUser)
                                        IssueNotificationAsync(scheduledCallback.UserNotificationMessage, callbackId);

                                    scheduledCallback.Action(callbackId, scheduledCallback.Canceller.Token);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                            finally
                            {
                                // if this is a repeating callback, then we'll need to reset the cancellation token source with a new instance, since they cannot be reused. if
                                // we enter the _idCallback lock before CancelRaisedCallback, then the next raise will be cancelled. if CancelRaisedCallback enters the 
                                // _idCallback lock first, then the cancellation token source will be overwritten here and the cancel will not have any effect. however,
                                // the latter case is a reasonable outcome, since the purpose of CancelRaisedCallback is to terminate any callbacks that are currently in 
                                // progress, and the current callback is no longer in progress. if the desired outcome is complete discontinuation of the repeating callback
                                // then UnscheduleRepeatingCallback should be used -- this method first cancels any raised callbacks and then removes the callback entirely.
                                try
                                {
                                    if (repeating)
                                        lock (_idCallback)
                                            scheduledCallback.Canceller = new CancellationTokenSource();
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    Monitor.Exit(scheduledCallback.Action);
                                }
                            }
                        }
                        else
                            _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") is already running. Not running again.", LoggingLevel.Normal, GetType());
                    
                        // if this was a one-time callback, remove it from our collection. it is no longer needed.
                        if (!repeating)
                            lock (_idCallback)
                                _idCallback.Remove(callbackId);                               

                        if (callback != null)
                            callback();
                    }
                    finally
                    {
                        // do this to ensure that the let-sleep is always called
                        LetDeviceSleep();
                    }

                }).Start();                           
        }

        /// <summary>
        /// Cancels a callback that has been raised and is currently executing.
        /// </summary>
        /// <param name="callbackId">Callback identifier.</param>
        public void CancelRaisedCallback(string callbackId)
        {
            lock (_idCallback)
            {
                ScheduledCallback scheduledCallback;
                if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
                    scheduledCallback.Canceller.Cancel();
            }
        }

        public void UnscheduleOneTimeCallback(string callbackId)
        {
            if (callbackId != null)
                lock (_idCallback)
                {
                    SensusServiceHelper.Get().Logger.Log("Unscheduling one-time callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());

                    CancelRaisedCallback(callbackId);
                    _idCallback.Remove(callbackId);

                    UnscheduleCallback(callbackId, false);
                }
        }

        public void UnscheduleRepeatingCallback(string callbackId)
        {                                      
            if (callbackId != null)
                lock (_idCallback)
                {
                    SensusServiceHelper.Get().Logger.Log("Unscheduling repeating callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());

                    CancelRaisedCallback(callbackId);
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
            FlashNotificationAsync(message, null);
        }

        public void FlashNotificationAsync(string message, Action callback)
        {
            // do not show flash notifications when unit testing, as they can disrupt UI scripting on iOS.
            #if !UNIT_TESTING
            ProtectedFlashNotificationAsync(message, callback);
            #endif
        }

        public void PromptForInputAsync(string windowTitle, Input input, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action<Input> callback)
        {
            PromptForInputsAsync(windowTitle, new Input[] { input }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, inputs =>
                {
                    if (inputs == null)
                        callback(null);
                    else
                        callback(inputs[0]);
                });
        }

        public void PromptForInputsAsync(string windowTitle, IEnumerable<Input> inputs, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action<List<Input>> callback)
        {
            InputGroup inputGroup = new InputGroup(windowTitle);

            foreach (Input input in inputs)
                inputGroup.Inputs.Add(input);

            PromptForInputsAsync(false, DateTimeOffset.MinValue, new InputGroup[] { inputGroup }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, null, inputGroups =>
                {
                    if (inputGroups == null)
                        callback(null);
                    else
                        callback(inputGroups.SelectMany(g => g.Inputs).ToList());
                });
        }

        public void PromptForInputsAsync(bool isReprompt, DateTimeOffset firstPromptTimestamp, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action postDisplayCallback, Action<IEnumerable<InputGroup>> callback)
        {
            new Thread(() =>
                {
                    if (inputGroups == null || inputGroups.Count() == 0 || inputGroups.All(inputGroup => inputGroup == null))
                    {
                        callback(inputGroups);
                        return;
                    }

                    // only one prompt can run at a time...enforce that here.
                    lock (PROMPT_FOR_INPUTS_LOCKER)
                    {
                        if (PROMPT_FOR_INPUTS_RUNNING)
                        {
                            _logger.Log("A prompt is already running. Dropping current prompt request.", LoggingLevel.Normal, GetType());
                            callback(inputGroups);
                            return;
                        }
                        else
                            PROMPT_FOR_INPUTS_RUNNING = true;
                    }

                    bool firstPageDisplay = true;

                    Stack<int> inputGroupNumBackStack = new Stack<int>();

                    for (int inputGroupNum = 0; inputGroups != null && inputGroupNum < inputGroups.Count() && !cancellationToken.GetValueOrDefault().IsCancellationRequested; ++inputGroupNum)
                    {
                        InputGroup inputGroup = inputGroups.ElementAt(inputGroupNum);

                        ManualResetEvent responseWait = new ManualResetEvent(false);

                        // run voice inputs by themselves, and only if the input group contains exactly one voice input.
                        if (inputGroup.Inputs.Count == 1 && inputGroup.Inputs[0] is VoiceInput)
                        {
                            VoiceInput voiceInput = inputGroup.Inputs[0] as VoiceInput;

                            if (voiceInput.Enabled && voiceInput.Display)
                            {
                                voiceInput.RunAsync(isReprompt, firstPromptTimestamp, response =>
                                    {                
                                        responseWait.Set();
                                    });
                            }
                            else
                                responseWait.Set();
                        }
                        else
                        {
                            BringToForeground();

                            Device.BeginInvokeOnMainThread(async () =>
                                {
                                    PromptForInputsPage promptForInputsPage = new PromptForInputsPage(inputGroup, inputGroupNum + 1, inputGroups.Count(), showCancelButton, nextButtonText, cancellationToken, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, result =>
                                        {
                                            if (result == PromptForInputsPage.Result.Cancel || result == PromptForInputsPage.Result.NavigateBackward && inputGroupNumBackStack.Count == 0)
                                                inputGroups = null;
                                            else if (result == PromptForInputsPage.Result.NavigateBackward)
                                                inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                            else
                                                inputGroupNumBackStack.Push(inputGroupNum);

                                            responseWait.Set();
                                        });

                                    // do not display prompts page under the following conditions:  1) there are no inputs displayed on it. 2) the cancellation 
                                    // token has requested a cancellation. if any of these conditions are true, set the wait handle and continue to the next input group.

                                    if (promptForInputsPage.DisplayedInputCount == 0)
                                    {
                                        // if we're on the final input group and no inputs were shown, then we're at the end and we're ready to submit the 
                                        // users' responses. first check that the user is ready to submit. if the user isn't ready, move back to the previous 
                                        // input group if there is one.
                                        if (inputGroupNum >= inputGroups.Count() - 1 &&
                                            !string.IsNullOrWhiteSpace(submitConfirmation) &&
                                            inputGroupNumBackStack.Count > 0 &&
                                            !(await App.Current.MainPage.DisplayAlert("Confirm", submitConfirmation, "Yes", "No")))
                                        {
                                            inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                        }

                                        responseWait.Set();
                                    }
                                    else if (cancellationToken.GetValueOrDefault().IsCancellationRequested)
                                        responseWait.Set();
                                    else
                                    {
                                        await App.Current.MainPage.Navigation.PushModalAsync(promptForInputsPage, firstPageDisplay);  // only animate the display for the first page

                                        firstPageDisplay = false;

                                        if (postDisplayCallback != null)
                                            postDisplayCallback();
                                    }                                    
                                });
                        }

                        responseWait.WaitOne();    
                    }

                    // at this point we're done showing pages to the user. anything that needs to happen below with GPS tagging or subsequently
                    // in the callback can happen concurrently with any calls that might happen to come into this method. if the callback
                    // calls into this method immediately, there could be a race condition between the call and a call from some other part of 
                    // the system. this is okay, as the latter call is always in a race condition anyway. if the imagined callback is beaten
                    // to its reentrant call of this method by a call from somewhere else in the system, the callback might be prevented from 
                    // executing; however, can't think of a place where this might happen with negative consequences.
                    PROMPT_FOR_INPUTS_RUNNING = false;

                    #region geotag input groups if the user didn't cancel and we've got input groups with inputs that are complete and lacking locations
                    if (inputGroups != null && inputGroups.Any(inputGroup => inputGroup.Geotag && inputGroup.Inputs.Any(input => input.Complete && (input.Latitude == null || input.Longitude == null))))
                    {
                        SensusServiceHelper.Get().Logger.Log("Geotagging input groups.", LoggingLevel.Normal, GetType());

                        try
                        {
                            Position currentPosition = GpsReceiver.Get().GetReading(cancellationToken.GetValueOrDefault());

                            if (currentPosition != null)
                                foreach (InputGroup inputGroup in inputGroups)
                                    if (inputGroup.Geotag)
                                        foreach (Input input in inputGroup.Inputs)
                                            if (input.Complete)
                                            {
                                                bool locationUpdated = false;

                                                if (input.Latitude == null)
                                                {
                                                    input.Latitude = currentPosition.Latitude;
                                                    locationUpdated = true;
                                                }

                                                if (input.Longitude == null)
                                                {
                                                    input.Longitude = currentPosition.Longitude;
                                                    locationUpdated = true;
                                                }

                                                if (locationUpdated)
                                                    input.LocationUpdateTimestamp = currentPosition.Timestamp;
                                            }
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Error geotagging input groups:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                    #endregion

                    callback(inputGroups);

                }).Start();
        }

        public void GetPositionsFromMapAsync(Xamarin.Forms.Maps.Position address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            Device.BeginInvokeOnMainThread(async () =>
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await App.Current.MainPage.Navigation.PushModalAsync(mapPage);
                });
        }

        public void GetPositionsFromMapAsync(string address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            Device.BeginInvokeOnMainThread(async () =>
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await App.Current.MainPage.Navigation.PushModalAsync(mapPage);
                });
        }

        public void TestHealth(string callbackId, CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                if (_stopped)
                    return;

                _logger.Log("Sensus health test is running.", LoggingLevel.Normal, GetType());

                foreach (Protocol protocol in _registeredProtocols)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    if (_runningProtocolIds.Contains(protocol.Id))
                        protocol.TestHealth(false);
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

        public string ConvertJsonForCrossPlatform(string json)
        {
            string newJSON;
            string typeName = GetType().Name;
            if (typeName == "AndroidSensusServiceHelper")
                newJSON = json.Replace("iOS", "Android").Replace("WinPhone", "Android");
            else if (typeName == "iOSSensusServiceHelper")
                newJSON = json.Replace("Android", "iOS").Replace("WinPhone", "iOS");
            else if (typeName == "WinPhone")
                newJSON = json.Replace("Android", "WinPhone").Replace("iOS", "WinPhone");
            else
                throw new SensusException("Attempted to convert JSON for unknown service helper type:  " + SensusServiceHelper.Get().GetType().FullName);

            if (newJSON == json)
                _logger.Log("No cross-platform conversion required for JSON.", LoggingLevel.Normal, GetType());
            else
            {
                _logger.Log("Performed cross-platform conversion of JSON.", LoggingLevel.Normal, GetType());
                json = newJSON;
            }

            return json;
        }

        public virtual void Dispose()
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to stop service helper:  " + ex.Message);
            }

            SINGLETON = null;
        }
    }
}

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
using Xamarin;
using System.Collections.ObjectModel;
using SensusUI;
using SensusUI.Inputs;
using Xamarin.Forms;
using SensusService.Exceptions;
using ZXing.Mobile;
using ZXing;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;
using SensusService.Probes.User.Scripts;

#if __IOS__
using XLabs.Platform.Device;
#endif

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper
    {
        #region static members

        private static SensusServiceHelper SINGLETON;
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";
        public const string SENSUS_CALLBACK_ID_KEY = "SENSUS-CALLBACK-ID";
        public const string SENSUS_CALLBACK_REPEATING_KEY = "SENSUS-CALLBACK-REPEATING";
        public const string SENSUS_CALLBACK_REPEAT_DELAY_KEY = "SENSUS-CALLBACK-REPEAT-DELAY";
        public const string SENSUS_CALLBACK_REPEAT_LAG_KEY = "SENSUS-CALLBACK-REPEAT-LAG";
        public const string NOTIFICATION_ID_KEY = "ID";
        public const string PENDING_SURVEY_NOTIFICATION_ID = "PENDING-SURVEY-NOTIFICATION";
        public const int PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS = 60;
        protected const string XAMARIN_INSIGHTS_APP_KEY = "";
        private const string ENCRYPTION_KEY = "";
        public static readonly string SHARE_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
        private static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static readonly string SERIALIZATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");

#if DEBUG || UNIT_TESTING
        // test every 30 seconds in debug
        public const int HEALTH_TEST_DELAY_MS = 30000;
#elif RELEASE
        // test every 15 minutes in release
        public const int HEALTH_TEST_DELAY_MS = 900000;
#endif

        /// <summary>
        /// Health tests times are used to compute participation for the listening probes. They must
        /// be as tight as possible.
        /// </summary>
        private const bool HEALTH_TEST_REPEAT_LAG = false;

        public static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,

            #region need the following in order to deserialize protocols between OSs, whose objects contain different members (e.g., iOS service helper has ActivationId, which Android does not)
            Error = (o, e) =>
            {
                if (Get() != null)
                {
                    Get().Logger.Log("Failed to deserialize some part of the JSON:  " + e.ErrorContext.Error.ToString(), LoggingLevel.Normal, typeof(Protocol));
                    e.ErrorContext.Handled = true;
                }
            },

            MissingMemberHandling = MissingMemberHandling.Ignore,  // need to ignore missing members for cross-platform deserialization
            Formatting = Formatting.Indented  // must use indented formatting in order for cross-platform type conversion to work (depends on each "$type" name-value pair being on own line).
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
            if (SINGLETON == null)
            {
                Exception deserializeException;
                if (!TryDeserializeSingleton(out deserializeException))
                {
                    // we failed to deserialize. wait a bit and try again. but don't wait too long since we're holding up the 
                    // app-load sequence, which is not allowed to take too much time.
                    Thread.Sleep(5000);

                    if (!TryDeserializeSingleton(out deserializeException))
                    {
                        // we really couldn't deserialize the service helper! try to create a new service helper...
                        try
                        {
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

                        SINGLETON.Logger.Log("Repeatedly failed to deserialize service helper. Most recent exception:  " + deserializeException.Message, LoggingLevel.Normal, SINGLETON.GetType());
                        SINGLETON.Logger.Log("Created new service helper after failing to deserialize the old one.", LoggingLevel.Normal, SINGLETON.GetType());
                    }
                }
            }
            else
                SINGLETON.Logger.Log("Serivce helper already initialized. Nothing to do.", LoggingLevel.Normal, SINGLETON.GetType());
        }

        private static bool TryDeserializeSingleton(out Exception ex)
        {
            ex = null;
            string errorMessage = null;

            // read bytes
            byte[] encryptedJsonBytes = null;
            try
            {
                encryptedJsonBytes = ReadAllBytes(SERIALIZATION_PATH);
            }
            catch (Exception exception)
            {
                errorMessage = "Failed to read service helper file into byte array:  " + exception.Message;
                Console.Error.WriteLine(errorMessage);
            }

            if (encryptedJsonBytes != null)
            {
                // decrypt JSON
                string decryptedJSON = null;
                try
                {
                    decryptedJSON = Decrypt(encryptedJsonBytes);
                }
                catch (Exception exception)
                {
                    errorMessage = "Failed to decrypt service helper byte array (length=" + encryptedJsonBytes.Length + ") into JSON:  " + exception.Message;
                    Console.Error.WriteLine(errorMessage);
                }

                if (decryptedJSON != null)
                {
                    // deserialize service helper
                    try
                    {
                        SINGLETON = JsonConvert.DeserializeObject<SensusServiceHelper>(decryptedJSON, JSON_SERIALIZER_SETTINGS);
                    }
                    catch (Exception exception)
                    {
                        errorMessage = "Failed to deserialize service helper JSON (length=" + decryptedJSON.Length + ") into service helper:  " + exception.Message;
                        Console.Error.WriteLine(errorMessage);
                    }
                }
            }

            if (errorMessage != null)
                ex = new Exception(errorMessage);

            if (SINGLETON != null)
                SINGLETON.Logger.Log("Deserialized service helper with " + SINGLETON.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, SINGLETON.GetType());

            return SINGLETON != null;
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
            byte[] fileBytes = null;

            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileBytes = new byte[file.Length];
                byte[] blockBytes = new byte[1024];
                int blockBytesRead;
                int totalBytesRead = 0;
                while ((blockBytesRead = file.Read(blockBytes, 0, blockBytes.Length)) > 0)
                {
                    Array.Copy(blockBytes, 0, fileBytes, totalBytesRead, blockBytesRead);
                    totalBytesRead += blockBytesRead;
                }

                if (totalBytesRead != fileBytes.Length)
                    throw new Exception("Mismatch between file length (" + file.Length + ") and bytes read (" + totalBytesRead + ").");
            }

            return fileBytes;
        }

        public static double GetDirectorySizeMB(string directory)
        {
            double directorySizeMB = 0;

            foreach (string path in Directory.GetFiles(directory))
                directorySizeMB += GetFileSizeMB(path);

            return directorySizeMB;
        }

        public static double GetFileSizeMB(string path)
        {
            return new FileInfo(path).Length / (1024d * 1024d);
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

        private Logger _logger;
        private ObservableCollection<Protocol> _registeredProtocols;
        private List<string> _runningProtocolIds;
        private string _healthTestCallbackId;
        private Dictionary<string, ScheduledCallback> _idCallback;
        private SHA256Managed _hasher;
        private List<PointOfInterest> _pointsOfInterest;
        private MobileBarcodeScanner _barcodeScanner;
        private ZXing.Mobile.BarcodeWriter _barcodeWriter;
        private bool _flashNotificationsEnabled;

        // we use the following observable collection in ListViews within Sensus. this is not thread-safe,
        // so any write operations involving this collection should be performed on the UI thread.
        private ObservableCollection<Script> _scriptsToRun;

        private readonly object _shareFileLocker = new object();
        private readonly object _saveLocker = new object();

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
            get { return _runningProtocolIds; }
        }

        public List<PointOfInterest> PointsOfInterest
        {
            get { return _pointsOfInterest; }
        }

        [JsonIgnore]
        public MobileBarcodeScanner BarcodeScanner
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

        public bool FlashNotificationsEnabled
        {
            get
            {
                return _flashNotificationsEnabled;
            }
            set
            {
                _flashNotificationsEnabled = value;
            }
        }

        public ObservableCollection<Script> ScriptsToRun
        {
            get
            {
                return _scriptsToRun;
            }
        }

        [JsonIgnore]
        public float GpsDesiredAccuracyMeters
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_ACCURACY_METERS : runningProtocols.Min(p => p.GpsDesiredAccuracyMeters);
            }
        }

        [JsonIgnore]
        public int GpsMinTimeDelayMS
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_MIN_TIME_DELAY_MS : runningProtocols.Min(p => p.GpsMinTimeDelayMS);
            }
        }

        [JsonIgnore]
        public float GpsMinDistanceDelayMeters
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS : runningProtocols.Min(p => p.GpsMinDistanceDelayMeters);
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

        [JsonIgnore]
        protected abstract bool IsOnMainThread { get; }

        [JsonIgnore]
        public abstract string Version { get; }

        #region iOS GPS listener settings

#if __IOS__

        [JsonIgnore]
        public bool GpsPauseLocationUpdatesAutomatically
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsPauseLocationUpdatesAutomatically);
            }
        }

        [JsonIgnore]
        public ActivityType GpsActivityType
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 || runningProtocols.Select(p => p.GpsPauseActivityType).Distinct().Count() > 1 ? ActivityType.Other : runningProtocols.First().GpsPauseActivityType;
            }
        }

        [JsonIgnore]
        public bool GpsListenForSignificantChanges
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsListenForSignificantChanges);
            }
        }

        [JsonIgnore]
        public bool GpsDeferLocationUpdates
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsDeferLocationUpdates);
            }
        }

        [JsonIgnore]
        public float GpsDeferralDistanceMeters
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? -1 : runningProtocols.Min(p => p.GpsDeferralDistanceMeters);
            }
        }

        [JsonIgnore]
        public float GpsDeferralTimeMinutes
        {
            get
            {
                List<Protocol> runningProtocols = GetRunningProtocols();
                return runningProtocols.Count == 0 ? -1 : runningProtocols.Min(p => p.GpsDeferralTimeMinutes);
            }
        }

#endif

        #endregion

        #endregion

        protected SensusServiceHelper()
        {
            if (SINGLETON != null)
                throw new SensusException("Attempted to construct new service helper when singleton already existed.");

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

            _flashNotificationsEnabled = true;
            _scriptsToRun = new ObservableCollection<Script>();

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

        public void RunOnMainThread(Action action)
        {
            if (IsOnMainThread)
                action();
            else
                RunOnMainThreadNative(action);
        }

        #region platform-specific methods. this functionality cannot be implemented in a cross-platform way. it must be done separately for each platform.

        protected abstract void InitializeXamarinInsights();

        protected abstract void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag);

        protected abstract void ScheduleOneTimeCallback(string callbackId, int delayMS);

        protected abstract void UnscheduleCallbackPlatformSpecific(string callbackId);

        protected abstract void ProtectedFlashNotificationAsync(string message, bool flashLaterIfNotVisible, TimeSpan duration, Action callback);

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject, string mimeType);

        public abstract void SendEmailAsync(string toAddress, string subject, string message);

        public abstract void TextToSpeechAsync(string text, Action callback);

        public abstract void RunVoicePromptAsync(string prompt, Action postDisplayCallback, Action<string> callback);

        public abstract void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate);

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

        public virtual bool EnableBluetooth(bool lowEnergy, string rationale)
        {
            try
            {
                AssertNotOnMainThread(GetType() + " EnableBluetooth");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual bool DisableBluetooth(bool reenable, bool lowEnergy, string rationale)
        {
            try
            {
                AssertNotOnMainThread(GetType() + " DisableBluetooth");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected abstract void RunOnMainThreadNative(Action action);

        #endregion

        #region add/remove running protocol ids

        public void AddRunningProtocolId(string id)
        {
            lock (_runningProtocolIds)
            {
                if (!_runningProtocolIds.Contains(id))
                    _runningProtocolIds.Add(id);

                if (_healthTestCallbackId == null)
                {
                    ScheduledCallback healthTestCallback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
                    {
                        List<Protocol> protocolsToTest = new List<Protocol>();

                        lock (_registeredProtocols)
                        {
                            lock (_runningProtocolIds)
                            {
                                foreach (Protocol protocol in _registeredProtocols)
                                    if (_runningProtocolIds.Contains(protocol.Id))
                                        protocolsToTest.Add(protocol);
                            }
                        }

                        foreach (Protocol protocolToTest in protocolsToTest)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            _logger.Log("Sensus health test for protocol \"" + protocolToTest.Name + "\" is running on callback " + callbackId + ".", LoggingLevel.Normal, GetType());

                            await protocolToTest.TestHealthAsync(false, cancellationToken);
                        }

                    }, "Test Health", TimeSpan.FromMinutes(1));

                    _healthTestCallbackId = ScheduleRepeatingCallback(healthTestCallback, HEALTH_TEST_DELAY_MS, HEALTH_TEST_DELAY_MS, HEALTH_TEST_REPEAT_LAG);
                }
            }
        }

        public void RemoveRunningProtocolId(string id)
        {
            lock (_runningProtocolIds)
            {
                _runningProtocolIds.Remove(id);

                if (_runningProtocolIds.Count == 0)
                {
                    UnscheduleCallback(_healthTestCallbackId);
                    _healthTestCallbackId = null;
                }
            }
        }

        public bool ProtocolShouldBeRunning(Protocol protocol)
        {
            return _runningProtocolIds.Contains(protocol.Id);
        }

        public List<Protocol> GetRunningProtocols()
        {
            return _registeredProtocols.Where(p => p.Running).ToList();
        }

        #endregion

        public void SaveAsync(Action callback = null)
        {
            new Thread(() =>
                {
                    Save();

                    if (callback != null)
                        callback();

                }).Start();
        }

        public void Save()
        {
            lock (_saveLocker)
            {
                _logger.Log("Serializing service helper.", LoggingLevel.Normal, GetType());

                try
                {
                    string serviceHelperJSON = JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS);
                    byte[] encryptedBytes = Encrypt(serviceHelperJSON);
                    File.WriteAllBytes(SERIALIZATION_PATH, encryptedBytes);

                    _logger.Log("Serialized service helper with " + _registeredProtocols.Count + " protocols.", LoggingLevel.Normal, GetType());
                }
                catch (Exception ex)
                {
                    _logger.Log("Failed to serialize Sensus service helper:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                // ensure that all logged messages make it into the file.
                _logger.CommitMessageBuffer();
            }
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
            lock (_registeredProtocols)
            {
                foreach (Protocol protocol in _registeredProtocols)
                    if (!protocol.Running && _runningProtocolIds.Contains(protocol.Id))
                        protocol.Start();
            }
        }

        public void RegisterProtocol(Protocol protocol)
        {
            lock (_registeredProtocols)
            {
                if (!_registeredProtocols.Contains(protocol))
                    _registeredProtocols.Add(protocol);
            }
        }

        public void AddScriptToRun(Script script, RunMode runMode)
        {
            RunOnMainThread(() =>
            {
                bool add = true;

                List<Script> scriptsWithSameParent = _scriptsToRun.Where(s => s.SharesParentScriptWith(script)).ToList();

                if (scriptsWithSameParent.Count > 0)
                {
                    if (runMode == RunMode.SingleKeepOldest)
                        add = false;
                    else if (runMode == RunMode.SingleUpdate)
                        foreach (Script scriptWithSameParent in scriptsWithSameParent)
                            _scriptsToRun.Remove(scriptWithSameParent);
                }

                if (add)
                {
                    _scriptsToRun.Insert(0, script);
                    IssuePendingSurveysNotificationAsync(true, true);
                }
            });
        }

        public void RemoveScriptToRun(Script script)
        {
            RunOnMainThread(() =>
            {
                if (_scriptsToRun.Remove(script))
                    IssuePendingSurveysNotificationAsync(false, false);
            });
        }

        public void RemoveScriptsToRun(ScriptRunner runner)
        {
            RunOnMainThread(() =>
            {
                bool removed = false;

                foreach (Script scriptFromRunner in _scriptsToRun.Where(script => ReferenceEquals(script.Runner, runner)).ToList())
                    if (_scriptsToRun.Remove(scriptFromRunner))
                        removed = true;

                if (removed)
                    IssuePendingSurveysNotificationAsync(false, false);
            });
        }

        public void RemoveOldScripts(bool issueNotification)
        {
            RunOnMainThread(() =>
            {
                bool removed = false;

                foreach (Script script in _scriptsToRun.ToList())
                    if (script.Runner.MaximumAgeMinutes.HasValue && script.Age.TotalMinutes >= script.Runner.MaximumAgeMinutes && _scriptsToRun.Remove(script))
                        removed = true;

                if (removed && issueNotification)
                    IssuePendingSurveysNotificationAsync(false, false);
            });
        }

        public void IssuePendingSurveysNotificationAsync(bool playSound, bool vibrate)
        {
            RemoveOldScripts(false);

            string message = null;

            int scriptsToRun = _scriptsToRun.Count;
            if (scriptsToRun > 0)
                message = "You have " + scriptsToRun + " pending survey" + (scriptsToRun == 1 ? "" : "s") + ".";

            IssueNotificationAsync(message, PENDING_SURVEY_NOTIFICATION_ID, playSound, vibrate);
        }

        public void ClearPendingSurveysNotificationAsync()
        {
            IssueNotificationAsync(null, PENDING_SURVEY_NOTIFICATION_ID, false, false);
        }

        #region callback scheduling

        public string ScheduleRepeatingCallback(ScheduledCallback callback, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback);
                ScheduleRepeatingCallback(callbackId, initialDelayMS, repeatDelayMS, repeatLag);
                return callbackId;
            }
        }

        public string ScheduleOneTimeCallback(ScheduledCallback callback, int delayMS)
        {
            lock (_idCallback)
            {
                string callbackId = AddCallback(callback);
                ScheduleOneTimeCallback(callbackId, delayMS);
                return callbackId;
            }
        }

        private string AddCallback(ScheduledCallback callback)
        {
            lock (_idCallback)
            {
                // treat the callback as if it were brand new, even if it might have been previously used (e.g., if it's being reschedueld). set a
                // new ID and cancellation token.
                callback.Id = Guid.NewGuid().ToString();
                callback.Canceller = new CancellationTokenSource();
                _idCallback.Add(callback.Id, callback);
                return callback.Id;
            }
        }

        public bool CallbackIsScheduled(string callbackId)
        {
            lock (_idCallback)
            {
                return _idCallback.ContainsKey(callbackId);
            }
        }

        public string GetCallbackUserNotificationMessage(string callbackId)
        {
            lock (_idCallback)
            {
                if (_idCallback.ContainsKey(callbackId))
                    return _idCallback[callbackId].UserNotificationMessage;
                else
                    return null;
            }
        }

        public string GetCallbackNotificationId(string callbackId)
        {
            lock (_idCallback)
            {
                if (_idCallback.ContainsKey(callbackId))
                    return _idCallback[callbackId].NotificationId;
                else
                    return null;
            }
        }

        public string RescheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            lock (_idCallback)
            {
                ScheduledCallback scheduledCallback;
                if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
                {
                    UnscheduleCallback(callbackId);
                    return ScheduleRepeatingCallback(scheduledCallback, initialDelayMS, repeatDelayMS, repeatLag);
                }
                else
                    return null;
            }
        }

        public void RaiseCallbackAsync(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            DateTime callbackStartTime = DateTime.Now;

            new Thread(async () =>
            {
                try
                {
                    ScheduledCallback scheduledCallback = null;

                    lock (_idCallback)
                    {
                        // do we have callback information for the passed callbackId? we might not, in the case where the callback is canceled by the user and the system fires it subsequently.
                        if (!_idCallback.TryGetValue(callbackId, out scheduledCallback))
                        {
                            _logger.Log("Callback " + callbackId + " is not valid. Unscheduling.", LoggingLevel.Normal, GetType());
                            UnscheduleCallback(callbackId);
                        }
                    }

                    if (scheduledCallback != null)
                    {
                        // the same callback action cannot be run multiple times concurrently. drop the current callback if it's already running. multiple
                        // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until it has executed.
                        bool actionAlreadyRunning = true;
                        lock (scheduledCallback)
                        {
                            if (!scheduledCallback.Running)
                            {
                                actionAlreadyRunning = false;
                                scheduledCallback.Running = true;
                            }
                        }

                        if (actionAlreadyRunning)
                            _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") is already running. Not running again.", LoggingLevel.Normal, GetType());
                        else
                        {
                            try
                            {
                                if (scheduledCallback.Canceller.IsCancellationRequested)
                                    _logger.Log("Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") was cancelled before it was raised.", LoggingLevel.Normal, GetType());
                                else
                                {
                                    _logger.Log("Raising callback \"" + scheduledCallback.Name + "\" (" + callbackId + ").", LoggingLevel.Normal, GetType());

                                    if (notifyUser)
                                        IssueNotificationAsync(scheduledCallback.UserNotificationMessage, callbackId, true, true);

                                    // if the callback specified a timeout, request cancellation at the specified time.
                                    if (scheduledCallback.CallbackTimeout.HasValue)
                                        scheduledCallback.Canceller.CancelAfter(scheduledCallback.CallbackTimeout.Value);

                                    await scheduledCallback.Action(callbackId, scheduledCallback.Canceller.Token, letDeviceSleepCallback);
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMessage = "Callback \"" + scheduledCallback.Name + "\" (" + callbackId + ") failed:  " + ex.Message;
                                _logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                                SensusException.Report(errorMessage, ex);
                            }
                            finally
                            {
                                // the cancellation token source for the current callback might have been canceled. if this is a repeating callback then we'll need a new
                                // cancellation token source because they cannot be reset and we're going to use the same scheduled callback again for the next repeat. 
                                // if we enter the _idCallback lock before CancelRaisedCallback does, then the next raise will be cancelled. if CancelRaisedCallback enters the 
                                // _idCallback lock first, then the cancellation token source will be overwritten here and the cancel will not have any effect on the next 
                                // raise. the latter case is a reasonable outcome, since the purpose of CancelRaisedCallback is to terminate a callback that is currently in 
                                // progress, and the current callback is no longer in progress. if the desired outcome is complete discontinuation of the repeating callback
                                // then UnscheduleRepeatingCallback should be used -- this method first cancels any raised callbacks and then removes the callback entirely.
                                try
                                {
                                    if (repeating)
                                    {
                                        lock (_idCallback)
                                        {
                                            scheduledCallback.Canceller = new CancellationTokenSource();
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    // if we marked the callback as running, ensure that we unmark it (note we're nested within two finally blocks so
                                    // this will always execute). this will allow others to run the callback.
                                    lock (scheduledCallback)
                                    {
                                        scheduledCallback.Running = false;
                                    }

                                    // schedule callback again if it was a repeating callback and is still scheduled with a valid repeat delay
                                    if (repeating && CallbackIsScheduled(callbackId) && repeatDelayMS >= 0 && scheduleRepeatCallback != null)
                                    {
                                        DateTime nextCallbackTime;

                                        // if this repeating callback is allowed to lag, schedule the repeat from the current time.
                                        if (repeatLag)
                                            nextCallbackTime = DateTime.Now.AddMilliseconds(repeatDelayMS);
                                        else
                                        {
                                            // otherwise, schedule the repeat from the time at which the current callback was raised.
                                            nextCallbackTime = callbackStartTime.AddMilliseconds(repeatDelayMS);
                                        }

                                        scheduleRepeatCallback(nextCallbackTime);
                                    }
                                    else
                                        UnscheduleCallback(callbackId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = "Failed to raise callback:  " + ex.Message;

                    _logger.Log(errorMessage, LoggingLevel.Normal, GetType());

                    try
                    {
                        Insights.Report(new Exception(errorMessage, ex), Insights.Severity.Critical);
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    if (finishedCallback != null)
                        finishedCallback();
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
                {
                    scheduledCallback.Canceller.Cancel();
                    _logger.Log("Cancelled callback \"" + scheduledCallback.Name + "\" (" + callbackId + ").", LoggingLevel.Normal, GetType());
                }
                else
                    _logger.Log("Callback \"" + callbackId + "\" not present. Cannot cancel.", LoggingLevel.Normal, GetType());
            }
        }

        public void UnscheduleCallback(string callbackId)
        {
            if (callbackId != null)
                lock (_idCallback)
                {
                    _logger.Log("Unscheduling callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());

                    CancelRaisedCallback(callbackId);
                    _idCallback.Remove(callbackId);
                    UnscheduleCallbackPlatformSpecific(callbackId);
                }
        }

        #endregion

        public void TextToSpeechAsync(string text)
        {
            TextToSpeechAsync(text, () =>
                {
                });
        }

        /// <summary>
        /// Flashs the a notification.
        /// </summary>
        /// <returns>The notification async.</returns>
        /// <param name="message">Message.</param>
        /// <param name="flashLaterIfNotVisible">Flash later if not visible.</param>
        /// <param name="duration">Duration. Increments of 2 seconds are best displayed.</param>
        /// <param name="callback">Callback.</param>
        public void FlashNotificationAsync(string message, bool flashLaterIfNotVisible = true, TimeSpan? duration = null, Action callback = null)
        {
            // do not show flash notifications when unit testing, as they can disrupt UI scripting on iOS.
#if !UNIT_TESTING

            if (_flashNotificationsEnabled)
            {
                if (!duration.HasValue)
                    duration = TimeSpan.FromSeconds(2);

                ProtectedFlashNotificationAsync(message, flashLaterIfNotVisible, duration.Value, callback);
            }
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

            PromptForInputsAsync(null, new InputGroup[] { inputGroup }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, null, inputGroups =>
            {
                if (inputGroups == null)
                    callback(null);
                else
                    callback(inputGroups.SelectMany(g => g.Inputs).ToList());
            });
        }

        public void PromptForInputsAsync(DateTimeOffset? firstPromptTimestamp, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action postDisplayCallback, Action<IEnumerable<InputGroup>> callback)
        {
            new Thread(() =>
            {
                if (inputGroups == null || inputGroups.Count() == 0 || inputGroups.All(inputGroup => inputGroup == null))
                {
                    callback(inputGroups);
                    return;
                }

                bool firstPageDisplay = true;

                // keep a stack of input groups that were displayed so that the user can navigate backward. not all groups are displayed due to display
                // conditions, so we can't simply adjust the index into the input groups.
                Stack<int> inputGroupNumBackStack = new Stack<int>();

                for (int inputGroupNum = 0; inputGroups != null && inputGroupNum < inputGroups.Count() && !cancellationToken.GetValueOrDefault().IsCancellationRequested; ++inputGroupNum)
                {
                    InputGroup inputGroup = inputGroups.ElementAt(inputGroupNum);

                    ManualResetEvent responseWait = new ManualResetEvent(false);

                    try
                    {
                        // run voice inputs by themselves, and only if the input group contains exactly one input and that input is a voice input.
                        if (inputGroup.Inputs.Count == 1 && inputGroup.Inputs[0] is VoiceInput)
                        {
                            VoiceInput voiceInput = inputGroup.Inputs[0] as VoiceInput;

                            if (voiceInput.Enabled && voiceInput.Display)
                            {
                                // only run the post-display callback the first time a page is displayed. the caller expects the callback
                                // to fire only once upon first display.
                                voiceInput.RunAsync(firstPromptTimestamp, firstPageDisplay ? postDisplayCallback : null, response =>
                                {
                                    firstPageDisplay = false;
                                    responseWait.Set();
                                });
                            }
                            else
                                responseWait.Set();
                        }
                        else
                        {
                            BringToForeground();

                            RunOnMainThread(async () =>
                            {
                                // catch any exceptions from preparing and displaying the prompts page
                                try
                                {
                                    int stepNumber = inputGroupNum + 1;
                                    bool promptPagePopped = false;

                                    PromptForInputsPage promptForInputsPage = new PromptForInputsPage(inputGroup, stepNumber, inputGroups.Count(), inputGroupNumBackStack.Count > 0, showCancelButton, nextButtonText, cancellationToken, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, firstPromptTimestamp, async result =>
                                    {
                                        // catch any exceptions from navigating to the next page
                                        try
                                        {
                                            // the prompt page has finished and needs to be popped. either the user finished the page or the cancellation token did so, and there 
                                            // might be a race condition. lock down the navigation object and check whether the page was already popped. don't do it again.
                                            INavigation navigation = Application.Current.MainPage.Navigation;
                                            bool pageWasAlreadyPopped;
                                            lock (navigation)
                                            {
                                                pageWasAlreadyPopped = promptPagePopped;
                                                promptPagePopped = true;
                                            }

                                            if (!pageWasAlreadyPopped)
                                            {
                                                // we aren't doing anything else, so the top of the modal stack should be the prompt page; however, check to be sure.
                                                if (navigation.ModalStack.Count > 0 && navigation.ModalStack.Last() is PromptForInputsPage)
                                                {
                                                    _logger.Log("Popping prompt page with result:  " + result, LoggingLevel.Normal, GetType());

                                                    // animate pop if the user submitted or canceled
                                                    await navigation.PopModalAsync(stepNumber == inputGroups.Count() && result == PromptForInputsPage.Result.NavigateForward ||
                                                                                   result == PromptForInputsPage.Result.Cancel);
                                                }

                                                if (result == PromptForInputsPage.Result.Cancel)
                                                    inputGroups = null;
                                                else if (result == PromptForInputsPage.Result.NavigateBackward)
                                                    inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                                else
                                                    inputGroupNumBackStack.Push(inputGroupNum);  // keep the group in the back stack and move to the next group
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // report exception and set wait handle if anything goes wrong while processing the current input group.
                                            try
                                            {
                                                Insights.Report(ex, Insights.Severity.Critical);
                                            }
                                            catch { }
                                        }
                                        finally
                                        {
                                            // ensure that the response wait is always set
                                            responseWait.Set();
                                        }
                                    });

                                    // do not display prompts page under the following conditions:  1) there are no inputs displayed on it. 2) the cancellation 
                                    // token has requested a cancellation. if either of these conditions is true, set the wait handle and continue to the next input group.
                                    if (promptForInputsPage.DisplayedInputCount == 0)
                                    {
                                        // if we're on the final input group and no inputs were shown, then we're at the end and we're ready to submit the 
                                        // users' responses. first check that the user is ready to submit. if the user isn't ready then move back to the previous 
                                        // input group in the backstack, if there is one.
                                        if (inputGroupNum >= inputGroups.Count() - 1 && // this is the final input group
                                            inputGroupNumBackStack.Count > 0 && // there is an input group to go back to (the current one was not displayed)
                                            !string.IsNullOrWhiteSpace(submitConfirmation) && // we have a submit confirmation
                                            !(await Application.Current.MainPage.DisplayAlert("Confirm", submitConfirmation, "Yes", "No"))) // user is not ready to submit
                                        {
                                            inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                        }

                                        responseWait.Set();
                                    }
                                    // don't display page if we've been canceled
                                    else if (cancellationToken.GetValueOrDefault().IsCancellationRequested)
                                        responseWait.Set();
                                    else
                                    {
                                        // display page. only animate the display for the first page.
                                        await Application.Current.MainPage.Navigation.PushModalAsync(promptForInputsPage, firstPageDisplay);

                                        // only run the post-display callback the first time a page is displayed. the caller expects the callback
                                        // to fire only once upon first display.
                                        if (firstPageDisplay && postDisplayCallback != null)
                                            postDisplayCallback();

                                        firstPageDisplay = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        Insights.Report(ex, Insights.Severity.Critical);
                                    }
                                    catch { }

                                    // if anything bad happens, set the wait handle to ensure we get out of the prompt.
                                    responseWait.Set();
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // report exception and set wait handle if anything goes wrong while processing the current input group.
                        try
                        {
                            Insights.Report(ex, Insights.Severity.Critical);
                        }
                        catch { }

                        responseWait.Set();
                    }

                    responseWait.WaitOne();
                }

                // process the inputs if the user didn't cancel
                if (inputGroups != null)
                {
                    // set the submission timestamp. do this before GPS tagging since the latter could take a while and we want the timestamp to 
                    // reflect the time that the user hit submit.
                    DateTimeOffset submissionTimestamp = DateTimeOffset.UtcNow;
                    foreach (InputGroup inputGroup in inputGroups)
                        foreach (Input input in inputGroup.Inputs)
                            input.SubmissionTimestamp = submissionTimestamp;

                    #region geotag input groups if we've got input groups with inputs that are complete and lacking locations
                    if (inputGroups.Any(inputGroup => inputGroup.Geotag && inputGroup.Inputs.Any(input => input.Complete && (input.Latitude == null || input.Longitude == null))))
                    {
                        _logger.Log("Geotagging input groups.", LoggingLevel.Normal, GetType());

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
                            _logger.Log("Error geotagging input groups:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                    #endregion
                }

                callback(inputGroups);

            }).Start();
        }

        public void GetPositionsFromMapAsync(Xamarin.Forms.Maps.Position address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            RunOnMainThread(async () =>
            {
                if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                    FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
                else
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await Application.Current.MainPage.Navigation.PushModalAsync(mapPage);
                }
            });
        }

        public void GetPositionsFromMapAsync(string address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            RunOnMainThread(async () =>
            {
                if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                    FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
                else
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await Application.Current.MainPage.Navigation.PushModalAsync(mapPage);
                }
            });
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (_registeredProtocols)
            {
                protocol.Stop();
                _registeredProtocols.Remove(protocol);
            }
        }

        /// <summary>
        /// Gets the share path with an extension.
        /// </summary>
        /// <returns>The share path.</returns>
        /// <param name="extension">Extension (with or without preceding ".")</param>
        public string GetSharePath(string extension)
        {
            lock (_shareFileLocker)
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
            string currentTypeName = GetType().Name;

            StringBuilder convertedJSON = new StringBuilder(json.Length * 2);
            bool conversionPerformed = false;

            // run through each line in the JSON and modify .NET types appropriately. json.net escapes \r and \n when serializing, so we can safely split on these characters.
            foreach (string jsonLine in json.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (jsonLine.Trim().StartsWith("\"$type\":"))
                {
                    // convert platform namespace
                    string convertedJsonLine;
                    if (currentTypeName == "AndroidSensusServiceHelper")
                        convertedJsonLine = jsonLine.Replace("iOS", "Android").Replace("WinPhone", "Android");
                    else if (currentTypeName == "iOSSensusServiceHelper")
                        convertedJsonLine = jsonLine.Replace("Android", "iOS").Replace("WinPhone", "iOS");
                    else if (currentTypeName == "WinPhoneSensusServiceHelper")
                        convertedJsonLine = jsonLine.Replace("Android", "WinPhone").Replace("iOS", "WinPhone");
                    else
                        throw new SensusException("Attempted to convert JSON for unknown service helper type:  " + GetType().FullName);

                    if (convertedJsonLine != jsonLine)
                        conversionPerformed = true;

                    convertedJSON.AppendLine(convertedJsonLine);
                }
                else
                    convertedJSON.AppendLine(jsonLine);
            }

            if (conversionPerformed)
                _logger.Log("Performed cross-platform conversion of JSON.", LoggingLevel.Normal, GetType());
            else
                _logger.Log("No cross-platform conversion required for JSON.", LoggingLevel.Normal, GetType());

            return convertedJSON.ToString();
        }

        public Task<PermissionStatus> ObtainPermissionAsync(Permission permission)
        {
            return Task.Run(async () =>
            {
                string rationale = null;
                if (permission == Permission.Camera)
                    rationale = "Sensus uses the camera to scan participation barcodes. Sensus will not record images or video.";
                else if (permission == Permission.Location)
                    rationale = "Sensus uses GPS to collect location information for studies you have enrolled in.";
                else if (permission == Permission.Microphone)
                    rationale = "Sensus uses the microphone to collect sound level information for studies you have enrolled in. Sensus will not record audio.";
                else if (permission == Permission.Phone)
                    rationale = "Sensus monitors telephone call metadata for studies you have enrolled in. Sensus will not record audio from calls.";
                else if (permission == Permission.Sensors)
                    rationale = "Sensus uses movement sensors to collect various types of information for studies you have enrolled in.";
                else if (permission == Permission.Storage)
                    rationale = "Sensus must be able to write to your device's storage for proper operation. Please grant this permission.";

                if (await CrossPermissions.Current.CheckPermissionStatusAsync(permission) == PermissionStatus.Granted)
                    return PermissionStatus.Granted;
                else
                {
                    // the Permissions plugin requires a main activity to be present on android. ensure this below.
                    BringToForeground();

                    // display rationale for request to the user if needed
                    if (rationale != null && await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(permission))
                    {
                        ManualResetEvent rationaleDialogWait = new ManualResetEvent(false);

                        RunOnMainThread(async () =>
                        {
                            await (Application.Current as App).MainPage.DisplayAlert("Permission Request", "On the next screen, Sensus will request access to your device's " + permission.ToString().ToUpper() + ". " + rationale, "OK");
                            rationaleDialogWait.Set();
                        });

                        rationaleDialogWait.WaitOne();
                    }

                    // request permission from the user
                    PermissionStatus status = PermissionStatus.Unknown;
                    try
                    {
                        Dictionary<Permission, PermissionStatus> permissionStatus = await CrossPermissions.Current.RequestPermissionsAsync(new Permission[] { permission });

                        // it's happened that the returned dictionary doesn't contain an entry for the requested permission, so check for that(https://insights.xamarin.com/app/Sensus-Production/issues/903).a
                        if (!permissionStatus.TryGetValue(permission, out status))
                            throw new Exception("Permission status not returned for request:  " + permission);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log("Failed to obtain permission:  " + ex.Message, LoggingLevel.Normal, GetType());
                        status = PermissionStatus.Unknown;
                    }

                    return status;
                }
            });
        }

        /// <summary>
        /// Obtains a permission. Must not call this method from the UI thread, since it blocks waiting for prompts that run on the UI thread (deadlock). If it
        /// is necessary to call from the UI thread, call ObtainPermissionAsync instead.
        /// </summary>
        /// <returns>The permission status.</returns>
        /// <param name="permission">Permission.</param>
        public PermissionStatus ObtainPermission(Permission permission)
        {
            try
            {
                AssertNotOnMainThread(GetType() + " ObtainPermission");
            }
            catch (Exception)
            {
                return PermissionStatus.Unknown;
            }

            PermissionStatus status = PermissionStatus.Unknown;
            ManualResetEvent wait = new ManualResetEvent(false);

            new Thread(async () =>
            {
                status = await ObtainPermissionAsync(permission);
                wait.Set();

            }).Start();

            wait.WaitOne();

            return status;
        }

        public void AssertNotOnMainThread(string actionDescription)
        {
            if (IsOnMainThread)
                throw new SensusException("Attempted to execute on main thread:  " + actionDescription);
        }

        public void StopProtocols()
        {
            lock (_registeredProtocols)
            {
                _logger.Log("Stopping protocols.", LoggingLevel.Normal, GetType());

                foreach (Protocol protocol in _registeredProtocols)
                    if (protocol.Running)
                    {
                        try
                        {
                            protocol.Stop();
                        }
                        catch (Exception ex)
                        {
                            _logger.Log("Failed to stop protocol \"" + protocol.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
            }
        }
    }
}

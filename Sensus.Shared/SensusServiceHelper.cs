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
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xamarin.Forms;
using Newtonsoft.Json;

using Sensus.UI;
using Sensus.Probes;
using Sensus.Context;
using Sensus.UI.Inputs;
using Sensus.Concurrent;
using Sensus.Exceptions;
using Sensus.Probes.Location;
using Sensus.Probes.User.Scripts;

using Plugin.Permissions;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Sensus.Callbacks;
using ZXing;
using ZXing.Net.Mobile.Forms;
using ZXing.Mobile;
using WindowsAzure.Messaging;

namespace Sensus
{
    /// <summary>
    /// Provides platform-independent functionality.
    /// </summary>
    public abstract class SensusServiceHelper
    {
        #region static members
        private static SensusServiceHelper SINGLETON;
        public const int PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS = 60;
        private const string PENDING_SURVEY_NOTIFICATION_ID = "SENSUS-PENDING-SURVEY-NOTIFICATION";

        /// <summary>
        /// App Center key for Android app.
        /// </summary>
        public const string APP_CENTER_KEY_ANDROID = "";

        /// <summary>
        /// App Center key for iOS app.
        /// </summary>
        public const string APP_CENTER_KEY_IOS = "";

        /// <summary>
        /// The 64-character hex-encoded string for a 256-bit symmetric AES encryption key. Used to secure protocols for distribution. Can be generated with the following command:
        /// 
        ///     openssl enc -aes-256-cbc -k secret -P -md sha1
        /// 
        /// The above was adapted from:  https://www.ibm.com/support/knowledgecenter/SSLVY3_9.7.0/com.ibm.einstall.doc/topics/t_einstall_GenerateAESkey.html
        /// </summary>
        public const string ENCRYPTION_KEY = "";

        /// <summary>
        /// The build ID, used to tag each <see cref="Datum"/>.
        /// </summary>
        public const string BUILD_ID = "";

        public static readonly string SHARE_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
        private static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static readonly string SERIALIZATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");

#if DEBUG || UI_TESTING
        // test every 30 seconds in debug
        public static readonly TimeSpan HEALTH_TEST_DELAY = TimeSpan.FromSeconds(30);
#elif RELEASE
        // test every 60 minutes in release
        public static readonly TimeSpan HEALTH_TEST_DELAY = TimeSpan.FromMinutes(60);
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

            #region need the following in order to deserialize protocols between OSs, which have different probes, etc.
            Error = (o, e) =>
            {
                if (Get() != null)
                {
                    Get().Logger.Log("Failed to (de)serialize some part of the JSON:  " + e.ErrorContext.Error, LoggingLevel.Normal, typeof(SensusServiceHelper));
                    e.ErrorContext.Handled = true;
                }
            },

            // need to ignore missing members for cross-platform deserialization
            MissingMemberHandling = MissingMemberHandling.Ignore,

            // must use indented formatting in order for cross-platform type conversion to work (depends on each "$type" name-value pair being on own line).
            Formatting = Formatting.Indented
            #endregion
        };

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

            Exception deserializeException;
            if (!TryDeserializeSingleton(out deserializeException))
            {
                // we really couldn't deserialize the service helper! try to create a new service helper...
                try
                {
                    SINGLETON = createNew();
                }
                catch (Exception singletonCreationException)
                {
                    // report exception and crash app
                    string error = "Failed to construct service helper:  " + singletonCreationException.Message + Environment.NewLine + singletonCreationException.StackTrace;
                    Console.Error.WriteLine(error);
                    Exception exceptionToReport = new Exception(error);
                    SensusException.Report(exceptionToReport);
                    throw exceptionToReport;
                }

                SINGLETON.Logger.Log("Repeatedly failed to deserialize service helper. Most recent exception:  " + deserializeException.Message, LoggingLevel.Normal, SINGLETON.GetType());
                SINGLETON.Logger.Log("Created new service helper after failing to deserialize the old one.", LoggingLevel.Normal, SINGLETON.GetType());
            }
        }

        private static bool TryDeserializeSingleton(out Exception ex)
        {
            ex = null;
            try
            {
                byte[] encryptedJsonBytes;
                try
                {
                    encryptedJsonBytes = ReadAllBytes(SERIALIZATION_PATH);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to read service helper file into byte array:  {exception.Message}");
                }

                string decryptedJSON;
                try
                {
                    decryptedJSON = SensusContext.Current.SymmetricEncryption.DecryptToString(encryptedJsonBytes);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to decrypt service helper byte array (length={encryptedJsonBytes.Length}) into JSON:  {exception.Message}");
                }

                try
                {
                    SINGLETON = JsonConvert.DeserializeObject<SensusServiceHelper>(decryptedJSON, JSON_SERIALIZER_SETTINGS);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to deserialize service helper JSON (length={decryptedJSON.Length}) into service helper:  {exception.Message}");
                }
            }
            catch (Exception exception)
            {
                ex = exception;
                Console.Error.WriteLine(exception.Message);
            }

            SINGLETON?.Logger.Log("Deserialized service helper with " + SINGLETON.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, SINGLETON.GetType());

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
                {
                    throw new Exception("Mismatch between file length (" + file.Length + ") and bytes read (" + totalBytesRead + ").");
                }
            }

            return fileBytes;
        }

        public static double GetDirectorySizeMB(string directory)
        {
            double directorySizeMB = 0;

            foreach (string path in Directory.GetFiles(directory))
            {
                directorySizeMB += GetFileSizeMB(path);
            }

            return directorySizeMB;
        }

        public static double GetFileSizeMB(string path)
        {
            return new FileInfo(path).Length / Math.Pow(1024d, 2);
        }

        /// <remarks>
        /// For testing purposes only
        /// </remarks>
        public static void ClearSingleton()
        {
            SINGLETON = null;
        }

        #endregion

        private Logger _logger;
        private List<string> _runningProtocolIds;
        private ScheduledCallback _healthTestCallback;
        private SHA256Managed _hasher;
        private List<PointOfInterest> _pointsOfInterest;
        private BarcodeWriter _barcodeWriter;
        private bool _flashNotificationsEnabled;
        private ConcurrentObservableCollection<Protocol> _registeredProtocols;
        private ConcurrentObservableCollection<Script> _scriptsToRun;
        private readonly object _shareFileLocker = new object();
        private readonly object _saveLocker = new object();

        [JsonIgnore]
        public Logger Logger
        {
            get { return _logger; }
        }

        public ConcurrentObservableCollection<Protocol> RegisteredProtocols
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
        public BarcodeWriter BarcodeWriter
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

        public ConcurrentObservableCollection<Script> ScriptsToRun
        {
            get
            {
                return _scriptsToRun;
            }
        }

        [JsonIgnore]
        public abstract string PushNotificationToken { get; set; }

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
        public abstract float BatteryChargePercent { get; }

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

        [JsonIgnore]
        public abstract string DeviceManufacturer { get; }

        [JsonIgnore]
        public abstract string DeviceModel { get; }

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

        #region Constructors
        [JsonConstructor]
        protected SensusServiceHelper()
        {
            if (SINGLETON != null)
            {
                throw SensusException.Report("Attempted to construct new service helper when singleton already existed.");
            }

            _registeredProtocols = new ConcurrentObservableCollection<Protocol>();
            _scriptsToRun = new ConcurrentObservableCollection<Script>();
            _runningProtocolIds = new List<string>();
            _hasher = new SHA256Managed();
            _pointsOfInterest = new List<PointOfInterest>();
            _barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 500,
                    Width = 500
                }
            };

            _flashNotificationsEnabled = true;

            if (!Directory.Exists(SHARE_DIRECTORY))
            {
                Directory.CreateDirectory(SHARE_DIRECTORY);
            }

#if DEBUG || UI_TESTING
            LoggingLevel loggingLevel = LoggingLevel.Debug;
#elif RELEASE
            LoggingLevel loggingLevel = LoggingLevel.Normal;
#else
#error "Unrecognized configuration."
#endif

            _logger = new Logger(LOG_PATH, loggingLevel, Console.Error);
            _logger.Log("Log file started at \"" + LOG_PATH + "\".", LoggingLevel.Normal, GetType());
        }
        #endregion

        public string GetHash(string s)
        {
            if (s == null)
            {
                return null;
            }

            StringBuilder hashBuilder = new StringBuilder();
            foreach (byte b in _hasher.ComputeHash(Encoding.UTF8.GetBytes(s)))
            {
                hashBuilder.Append(b.ToString("x"));
            }

            return hashBuilder.ToString();
        }

        #region platform-specific methods. this functionality cannot be implemented in a cross-platform way. it must be done separately for each platform. we are gradually migrating this functionality into the ISensusContext object.

        protected abstract Task ProtectedFlashNotificationAsync(string message);

        public abstract Task PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract Task ShareFileAsync(string path, string subject, string mimeType);

        public abstract Task SendEmailAsync(string toAddress, string subject, string message);

        public abstract Task TextToSpeechAsync(string text);

        public abstract Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback);

        public abstract void KeepDeviceAwake();

        public abstract void LetDeviceSleep();

        public abstract Task BringToForegroundAsync();

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

        #endregion

        #region add/remove running protocol ids

        public void AddRunningProtocolId(string id)
        {
            lock (_runningProtocolIds)
            {
                if (!_runningProtocolIds.Contains(id))
                {
                    _runningProtocolIds.Add(id);

#if __ANDROID__
                    (this as Android.IAndroidSensusServiceHelper).ReissueForegroundServiceNotification();
#endif
                }

                if (_healthTestCallback == null)
                {
                    _healthTestCallback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
                    {
                        // get protocols to test (those that should be running)
                        List<Protocol> protocolsToTest = _registeredProtocols.Where(protocol =>
                        {
                            lock (_runningProtocolIds)
                            {
                                return _runningProtocolIds.Contains(protocol.Id);
                            }

                        }).ToList();

                        // test protocols
                        foreach (Protocol protocolToTest in protocolsToTest)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            _logger.Log("Sensus health test for protocol \"" + protocolToTest.Name + "\" is running on callback " + callbackId + ".", LoggingLevel.Normal, GetType());

                            await protocolToTest.TestHealthAsync(false, cancellationToken);
                        }

                        // test the callback scheduler itself
                        SensusContext.Current.CallbackScheduler.TestHealth();

                    }, HEALTH_TEST_DELAY, HEALTH_TEST_DELAY, HEALTH_TEST_REPEAT_LAG, "HEALTH-TEST", GetType().FullName, null, TimeSpan.FromMinutes(1));

                    SensusContext.Current.CallbackScheduler.ScheduleCallback(_healthTestCallback);
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
                    SensusContext.Current.CallbackScheduler.UnscheduleCallback(_healthTestCallback);
                    _healthTestCallback = null;
                }

#if __ANDROID__
                (this as Android.IAndroidSensusServiceHelper).ReissueForegroundServiceNotification();
#endif
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

        public Task SaveAsync()
        {
            return Task.Run(() =>
            {
                Save();
            });
        }

        public void Save()
        {
            lock (_saveLocker)
            {
                _logger.Log("Serializing service helper.", LoggingLevel.Normal, GetType());

                try
                {
                    string serviceHelperJSON = JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS);
                    byte[] encryptedBytes = SensusContext.Current.SymmetricEncryption.Encrypt(serviceHelperJSON);
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

        public Task StartAsync()
        {
            return Task.Run(() =>
            {
                Start();
            });
        }

        /// <summary>
        /// Starts platform-independent service functionality, including protocols that should be running. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void Start()
        {
            foreach (Protocol registeredProtocol in _registeredProtocols)
            {
                if (!registeredProtocol.Running && _runningProtocolIds.Contains(registeredProtocol.Id))
                {
                    registeredProtocol.Start();
                }
            }
        }

        public void RegisterProtocol(Protocol protocol)
        {
            if (!_registeredProtocols.Contains(protocol))
            {
                _registeredProtocols.Add(protocol);
            }
        }

        public void AddScript(Script script, RunMode runMode)
        {
            // scripts can be added from several threads, particularly on ios when several script runs can execute concurrently when
            // the user opens the app. execute all additions to the _scriptsToRun collection on the main thread for safety.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // shuffle input groups and inputs if needed
                Random random = new Random();
                if (script.Runner.ShuffleInputGroups)
                {
                    random.Shuffle(script.InputGroups);
                }

                // shuffle inputs in groups if needed
                foreach (InputGroup inputGroup in script.InputGroups)
                {
                    if (inputGroup.ShuffleInputs)
                    {
                        random.Shuffle(inputGroup.Inputs);
                    }
                }

                bool modifiedScriptsToRun = false;

                if (runMode == RunMode.Multiple)
                {
                    _scriptsToRun.Insert(GetScriptIndex(script), script);
                    modifiedScriptsToRun = true;
                }
                else
                {
                    List<Script> scriptsFromSameRunner = _scriptsToRun.Where(scriptToRun => scriptToRun.Runner.Script.Id == script.Runner.Script.Id).ToList();
                    scriptsFromSameRunner.Add(script);

                    Script scriptToKeep = null;
                    List<Script> scriptsToRemove = null;

                    if (runMode == RunMode.SingleKeepOldest)
                    {
                        scriptToKeep = scriptsFromSameRunner.First();
                        scriptsToRemove = scriptsFromSameRunner.Skip(1).ToList();
                    }
                    else if (runMode == RunMode.SingleKeepNewest)
                    {
                        scriptToKeep = scriptsFromSameRunner.Last();
                        scriptsToRemove = scriptsFromSameRunner.Take(scriptsFromSameRunner.Count - 1).ToList();
                    }
                    else
                    {
                        SensusException.Report("Unrecognized RunMode:  " + runMode);
                        return;
                    }

                    foreach (Script scriptToRemove in scriptsToRemove)
                    {
                        if (_scriptsToRun.Remove(scriptToRemove))
                        {
                            modifiedScriptsToRun = true;
                        }
                    }

                    if (!_scriptsToRun.Contains(scriptToKeep))
                    {
                        _scriptsToRun.Insert(GetScriptIndex(scriptToKeep), scriptToKeep);
                        modifiedScriptsToRun = true;
                    }
                }

                if (modifiedScriptsToRun)
                {
                    IssuePendingSurveysNotificationAsync(script.Runner.Probe.Protocol, true);
                }
            });
        }

        public void RemoveScript(Script script)
        {
            RemoveScripts(true, script);
        }

        public void RemoveScriptsForRunner(ScriptRunner runner)
        {
            RemoveScripts(true, _scriptsToRun.Where(script => script.Runner == runner).ToArray());
        }

        public void RemoveExpiredScripts(bool issueNotification)
        {
            RemoveScripts(issueNotification, _scriptsToRun.Where(s => s.Expired).ToArray());
        }

        public void ClearScripts()
        {
            _scriptsToRun.Clear();
            IssuePendingSurveysNotificationAsync(null, false);
        }

        /// <summary>
        /// Issues the pending surveys notification.
        /// </summary>
        /// <param name="protocol">Protocol used to check for alert exclusion time windows. </param>
        /// <param name="alertUser">If set to <c>true</c> alert user using sound and/or vibration.</param>
        public void IssuePendingSurveysNotificationAsync(Protocol protocol, bool alertUser)
        {
            RemoveExpiredScripts(false);

            int numScriptsToRun = _scriptsToRun.Count;

            if (numScriptsToRun == 0)
            {
                ClearPendingSurveysNotificationAsync();
            }
            else
            {
                string s = numScriptsToRun == 1 ? "" : "s";
                string pendingSurveysTitle = numScriptsToRun == 0 ? null : $"You have {numScriptsToRun} pending survey{s}.";
                DateTime? nextExpirationDate = _scriptsToRun.Select(script => script.ExpirationDate).Where(expirationDate => expirationDate.HasValue).OrderBy(expirationDate => expirationDate).FirstOrDefault();
                string nextExpirationMessage = nextExpirationDate == null ? (numScriptsToRun == 1 ? "This survey does" : "These surveys do") + " not expire." : "Next expiration:  " + nextExpirationDate.Value.ToShortDateString() + " at " + nextExpirationDate.Value.ToShortTimeString();
                SensusContext.Current.Notifier.IssueNotificationAsync(pendingSurveysTitle, nextExpirationMessage, PENDING_SURVEY_NOTIFICATION_ID, protocol, alertUser, DisplayPage.PendingSurveys);
            }
        }

        public void ClearPendingSurveysNotificationAsync()
        {
            SensusContext.Current.Notifier.CancelNotification(PENDING_SURVEY_NOTIFICATION_ID);
        }

        /// <summary>
        /// Flashes a notification.
        /// </summary>
        /// <returns>The notification async.</returns>
        /// <param name="message">Message.</param>
        public Task FlashNotificationAsync(string message)
        {
            // do not show flash notifications when UI testing, as they can disrupt UI scripting on iOS.
#if !UI_TESTING
            if (_flashNotificationsEnabled)
            {
                return ProtectedFlashNotificationAsync(message);
            }
            else
            {
                return Task.CompletedTask;
            }
#endif
        }

        public Task<string> ScanQrCodeAsync(string resultPrefix)
        {
            return Task.Run(() =>
            {
                string result = null;
                ManualResetEvent resultWait = new ManualResetEvent(false);

                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    // we've seen exceptions where we don't ask for permission, leaving this up to the ZXing library
                    // to take care of. the library does ask for permission, but if it's denied we get an exception
                    // kicked back. ask explicitly here, and bail out if permission is not granted.
                    if (await ObtainPermissionAsync(Permission.Camera) != PermissionStatus.Granted)
                    {
                        resultWait.Set();
                        return;
                    }

                    // TODO:  there's a race condition bug in the scanning library:  https://github.com/Redth/ZXing.Net.Mobile/issues/717
                    // delaying a bit seems to fix it.
                    await Task.Delay(1000);

                    Button cancelButton = new Button
                    {
                        Text = "Cancel",
                        FontSize = 30
                    };

                    StackLayout scannerOverlay = new StackLayout
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        Padding = new Thickness(30),
                        Children = { cancelButton }
                    };

                    ZXingScannerPage barcodeScannerPage = new ZXingScannerPage(new MobileBarcodeScanningOptions
                    {
                        PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE }.ToList()
                                                                                       
                    }, scannerOverlay);

                    INavigation navigation = (Application.Current as App).DetailPage.Navigation;

                    Func<Task> CloseScannerPageAsync = new Func<Task>(() =>
                    {
                        return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            barcodeScannerPage.IsScanning = false;

                            // we've seen a strange race condition where the QR code input scanner button is 
                            // pressed, and in the above task delay the input group page is cancelled and 
                            // another UI button is hit before the scanner page comes up.
                            if (navigation.ModalStack.LastOrDefault() == barcodeScannerPage)
                            {
                                await navigation.PopModalAsync();
                            }

                            resultWait.Set();
                        });
                    });

                    cancelButton.Clicked += async (o, e) =>
                    {
                        await CloseScannerPageAsync();
                    };

                    barcodeScannerPage.OnScanResult += async r =>
                    {
                        if (resultPrefix == null || r.Text.StartsWith(resultPrefix))
                        {
                            result = r.Text.Substring(resultPrefix?.Length ?? 0).Trim();

                            await CloseScannerPageAsync();
                        }
                    };

                    barcodeScannerPage.Disappearing += (sender, e) =>
                    {
                        resultWait.Set();
                    };

                    await navigation.PushModalAsync(barcodeScannerPage);
                });

                resultWait.WaitOne();

                return result;
            });
        }

        public Task<Input> PromptForInputAsync(string windowTitle, Input input, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress)
        {
            return Task.Run(async () =>
            {
                List<Input> inputs = await PromptForInputsAsync(windowTitle, new[] { input }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress);
                return inputs?.First();
            });
        }

        public Task<List<Input>> PromptForInputsAsync(string windowTitle, IEnumerable<Input> inputs, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress)
        {
            return Task.Run(async () =>
            {
                InputGroup inputGroup = new InputGroup { Name = windowTitle };

                foreach (var input in inputs)
                {
                    inputGroup.Inputs.Add(input);
                }

                IEnumerable<InputGroup> inputGroups = await PromptForInputsAsync(null, new[] { inputGroup }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, null);

                return inputGroups?.SelectMany(g => g.Inputs).ToList();
            });
        }

        public Task<IEnumerable<InputGroup>> PromptForInputsAsync(DateTimeOffset? firstPromptTimestamp, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action postDisplayCallback)
        {
            return Task.Run(async () =>
            {
                bool firstPageDisplay = true;

                // keep a stack of input groups that were displayed so that the user can navigate backward. not all groups are displayed due to display
                // conditions, so we can't simply decrement the index to navigate backwards.
                Stack<int> inputGroupNumBackStack = new Stack<int>();

                for (int inputGroupNum = 0; inputGroups != null && inputGroupNum < inputGroups.Count() && !cancellationToken.GetValueOrDefault().IsCancellationRequested; ++inputGroupNum)
                {
                    InputGroup inputGroup = inputGroups.ElementAt(inputGroupNum);

                    // run voice inputs by themselves, and only if the input group contains exactly one input and that input is a voice input.
                    if (inputGroup.Inputs.Count == 1 && inputGroup.Inputs[0] is VoiceInput)
                    {
                        VoiceInput voiceInput = inputGroup.Inputs[0] as VoiceInput;

                        if (voiceInput.Enabled && voiceInput.Display)
                        {
                            try
                            {
                                // only run the post-display callback the first time a page is displayed. the caller expects the callback
                                // to fire only once upon first display.
                                await voiceInput.RunAsync(firstPromptTimestamp, firstPageDisplay ? postDisplayCallback : null);
                                firstPageDisplay = false;
                            }
                            catch (Exception ex)
                            {
                                SensusException.Report("Voice input failed to run.", ex);
                            }
                        }
                    }
                    else
                    {
                        await BringToForegroundAsync();

                        await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            int stepNumber = inputGroupNum + 1;

                            InputGroupPage inputGroupPage = new InputGroupPage(inputGroup, stepNumber, inputGroups.Count(), inputGroupNumBackStack.Count > 0, showCancelButton, nextButtonText, cancellationToken, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress);

                            // do not display prompts page under the following conditions:  
                            //
                            // 1) there are no inputs displayed on it
                            // 2) the cancellation token has requested a cancellation.
                            //
                            // if either of these conditions is true, continue to the next input group.

                            if (inputGroupPage.DisplayedInputCount == 0)
                            {
                                // if we're on the final input group and no inputs were shown, then we're at the end and we're ready to submit the 
                                // users' responses. first check that the user is ready to submit. if the user isn't ready then move back to the previous 
                                // input group in the backstack, if there is one.
                                if (inputGroupNum >= inputGroups.Count() - 1 &&                                                     // this is the final input group
                                    inputGroupNumBackStack.Count > 0 &&                                                             // there is an input group to go back to (the current one was not displayed)
                                    !string.IsNullOrWhiteSpace(submitConfirmation) &&                                               // we have a submit confirmation
                                    !(await Application.Current.MainPage.DisplayAlert("Confirm", submitConfirmation, "Yes", "No"))) // user is not ready to submit
                                {
                                    inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                }
                            }
                            // display the page if we've not been canceled
                            else if (!cancellationToken.GetValueOrDefault().IsCancellationRequested)
                            {
                                INavigation navigation = (Application.Current as App).DetailPage.Navigation;

                                // display page. only animate the display for the first page.
                                await navigation.PushModalAsync(inputGroupPage, firstPageDisplay);

                                // only run the post-display callback the first time a page is displayed. the caller expects the callback
                                // to fire only once upon first display.
                                if (firstPageDisplay)
                                {
                                    postDisplayCallback?.Invoke();
                                    firstPageDisplay = false;
                                }

                                InputGroupPage.NavigationResult navigationResult = await inputGroupPage.ResponseTask;

                                _logger.Log("Input group page navigation result:  " + navigationResult, LoggingLevel.Normal, GetType());

                                // animate pop if the user submitted or canceled. when doing this, reference the navigation context
                                // on the page rather than the local 'navigation' variable. this is necessary because the navigation
                                // context may have changed (e.g., if prior to the pop the user reopens the app via pending survey 
                                // notification.
                                await inputGroupPage.Navigation.PopModalAsync(navigationResult == InputGroupPage.NavigationResult.Submit || 
                                                                              navigationResult == InputGroupPage.NavigationResult.Cancel);

                                if (navigationResult == InputGroupPage.NavigationResult.Backward)
                                {
                                    // we only allow backward navigation when we have something on the back stack. so the following is safe.
                                    inputGroupNum = inputGroupNumBackStack.Pop() - 1;
                                }
                                else if (navigationResult == InputGroupPage.NavigationResult.Forward)
                                {
                                    // keep the group in the back stack.
                                    inputGroupNumBackStack.Push(inputGroupNum);
                                }
                                else if (navigationResult == InputGroupPage.NavigationResult.Cancel)
                                {
                                    inputGroups = null;
                                }

                                // there's nothing to do if the navigation result is submit, since we've finished the final
                                // group and we are about to return.
                            }
                        });
                    }
                }

                // process the inputs if the user didn't cancel
                if (inputGroups != null)
                {
                    // set the submission timestamp. do this before GPS tagging since the latter could take a while and we want the timestamp to 
                    // reflect the time that the user hit submit.
                    DateTimeOffset submissionTimestamp = DateTimeOffset.UtcNow;
                    foreach (InputGroup inputGroup in inputGroups)
                    {
                        foreach (Input input in inputGroup.Inputs)
                        {
                            input.SubmissionTimestamp = submissionTimestamp;
                        }
                    }

                    #region geotag input groups if we've got input groups with inputs that are complete and lacking locations
                    if (inputGroups.Any(inputGroup => inputGroup.Geotag && inputGroup.Inputs.Any(input => input.Complete && (input.Latitude == null || input.Longitude == null))))
                    {
                        _logger.Log("Geotagging input groups.", LoggingLevel.Normal, GetType());

                        try
                        {
                            Position currentPosition = GpsReceiver.Get().GetReading(cancellationToken.GetValueOrDefault(), true);

                            if (currentPosition != null)
                            {
                                foreach (InputGroup inputGroup in inputGroups)
                                {
                                    if (inputGroup.Geotag)
                                    {
                                        foreach (Input input in inputGroup.Inputs)
                                        {
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
                                                {
                                                    input.LocationUpdateTimestamp = currentPosition.Timestamp;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log("Error geotagging input groups:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                    #endregion
                }

                return inputGroups;
            });
        }

        public void GetPositionsFromMapAsync(Xamarin.Forms.Maps.Position address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                {
                    await FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
                }
                else
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await (Application.Current as App).DetailPage.Navigation.PushModalAsync(mapPage);
                }
            });
        }

        public void GetPositionsFromMapAsync(string address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                {
                    await FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
                }
                else
                {
                    MapPage mapPage = new MapPage(address, newPinName);

                    mapPage.Disappearing += (o, e) =>
                    {
                        callback(mapPage.Pins.Select(pin => pin.Position).ToList());
                    };

                    await (Application.Current as App).DetailPage.Navigation.PushModalAsync(mapPage);
                }
            });
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            _registeredProtocols.Remove(protocol);
            protocol.Stop();
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
                {
                    path = Path.Combine(SHARE_DIRECTORY, fileNum++ + (string.IsNullOrWhiteSpace(extension) ? "" : "." + extension.Trim('.')));
                }

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
                    {
                        convertedJsonLine = jsonLine.Replace("iOS", "Android").Replace("WinPhone", "Android");
                    }
                    else if (currentTypeName == "iOSSensusServiceHelper")
                    {
                        convertedJsonLine = jsonLine.Replace("Android", "iOS").Replace("WinPhone", "iOS");
                    }
                    else
                    {
                        throw SensusException.Report("Attempted to convert JSON for unknown service helper type:  " + GetType().FullName);
                    }

                    if (convertedJsonLine != jsonLine)
                    {
                        conversionPerformed = true;
                    }

                    convertedJSON.AppendLine(convertedJsonLine);
                }
                else
                {
                    convertedJSON.AppendLine(jsonLine);
                }
            }

            if (conversionPerformed)
            {
                _logger.Log("Performed cross-platform conversion of JSON.", LoggingLevel.Normal, GetType());
            }
            else
            {
                _logger.Log("No cross-platform conversion required for JSON.", LoggingLevel.Normal, GetType());
            }

            return convertedJSON.ToString();
        }

        public Task<PermissionStatus> ObtainPermissionAsync(Permission permission)
        {
            return Task.Run(() =>
            {
                return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    // the Permissions plugin requires a main activity to be present on android. ensure the activity is running
                    // before using the plugin.
                    await BringToForegroundAsync();

                    if (await CrossPermissions.Current.CheckPermissionStatusAsync(permission) == PermissionStatus.Granted)
                    {
                        return PermissionStatus.Granted;
                    }

                    string rationale = null;

                    if (permission == Permission.Calendar)
                    {
                        rationale = "Sensus collects calendar information for studies you enroll in.";
                    }
                    else if (permission == Permission.Camera)
                    {
                        rationale = "Sensus uses the camera to scan barcodes. Sensus will not record images or video.";
                    }
                    else if (permission == Permission.Contacts)
                    {
                        rationale = "Sensus collects calendar information for studies you enroll in.";
                    }
                    else if (permission == Permission.Location)
                    {
                        rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
                    }
                    else if (permission == Permission.LocationAlways)
                    {
                        rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
                    }
                    else if (permission == Permission.LocationWhenInUse)
                    {
                        rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
                    }
                    else if (permission == Permission.MediaLibrary)
                    {
                        rationale = "Sensus collects media for studies you enroll in.";
                    }
                    else if (permission == Permission.Microphone)
                    {
                        rationale = "Sensus uses the microphone to collect sound level information for studies you enroll in. Sensus will not record audio.";
                    }
                    else if (permission == Permission.Phone)
                    {
                        rationale = "Sensus collects call information for studies you enroll in. Sensus will not record audio from calls.";
                    }
                    else if (permission == Permission.Photos)
                    {
                        rationale = "Sensus collects photos for studies you enroll in.";
                    }
                    else if (permission == Permission.Reminders)
                    {
                        rationale = "Sensus collects reminder information for studies you enroll in.";
                    }
                    else if (permission == Permission.Sensors)
                    {
                        rationale = "Sensus uses movement sensors to collect information for studies you enroll in.";
                    }
                    else if (permission == Permission.Sms)
                    {
                        rationale = "Sensus collects text messages for studies you enroll in.";
                    }
                    else if (permission == Permission.Speech)
                    {
                        rationale = "Sensus uses the microphone for studies you enroll in.";
                    }
                    else if (permission == Permission.Storage)
                    {
                        rationale = "Sensus must be able to write to your device's storage for proper operation.";
                    }

                    if (rationale != null && await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(permission))
                    {
                        await Application.Current.MainPage.DisplayAlert("Permission Request", $"On the next screen, Sensus will request access to your device's {permission.ToString().ToUpper()}. {rationale}", "OK");
                    }

                    try
                    {
                        PermissionStatus permissionStatus;

                        // it's happened that the returned dictionary doesn't contain an entry for the requested permission, so check for that.
                        if (!(await CrossPermissions.Current.RequestPermissionsAsync(permission)).TryGetValue(permission, out permissionStatus))
                        {
                            throw new Exception($"Permission status not returned for request:  {permission}");
                        }

                        return permissionStatus;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Failed to obtain permission:  {ex.Message}", LoggingLevel.Normal, GetType());

                        return PermissionStatus.Unknown;
                    }
                });
            });
        }

        /// <summary>
        /// Obtains a permission synchronously. Must not call this method from the UI thread, since it blocks waiting for prompts 
        /// that run on the UI thread (deadlock). If it is necessary to call from the UI thread, call ObtainPermissionAsync instead.
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

            PermissionStatus permissionStatus = PermissionStatus.Unknown;
            ManualResetEvent wait = new ManualResetEvent(false);

            Task.Run(async () =>
            {
                permissionStatus = await ObtainPermissionAsync(permission);
                wait.Set();
            });

            wait.WaitOne();

            return permissionStatus;
        }

        public void AssertNotOnMainThread(string actionDescription)
        {
            if (IsOnMainThread)
            {
                throw SensusException.Report("Attempted to execute on main thread:  " + actionDescription);
            }
        }

        public virtual Task UpdatePushNotificationRegistrationsAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    // we should always have a token. if we do not, throw an exception and let the
                    // parent class override attempt to resolve the issue by getting a new token.
                    if (PushNotificationToken == null)
                    {
                        throw new UnsetPushNotificationTokenException();
                    }

                    // associate each notification hub with its protocols
                    Dictionary<Tuple<string, string>, List<Protocol>> hubSasProtocols = new Dictionary<Tuple<string, string>, List<Protocol>>();
                    foreach (Tuple<string, string, Protocol> hubSasProtocol in _registeredProtocols.Select(protocol => new Tuple<string, string, Protocol>(protocol.PushNotificationsHub, protocol.PushNotificationsSharedAccessSignature, protocol)))
                    {
                        if (!string.IsNullOrWhiteSpace(hubSasProtocol.Item1) && !string.IsNullOrWhiteSpace(hubSasProtocol.Item2))
                        {
                            Tuple<string, string> hubSas = new Tuple<string, string>(hubSasProtocol.Item1, hubSasProtocol.Item2);

                            if (!hubSasProtocols.ContainsKey(hubSas))
                            {
                                hubSasProtocols.Add(hubSas, new List<Protocol>());
                            }

                            hubSasProtocols[hubSas].Add(hubSasProtocol.Item3);
                        }
                    }


                    // update each notification hub's registration, using the protocols as tags to listen to.
                    await UpdatePushNotification(hubSasProtocols);
                }
                catch (UnsetPushNotificationTokenException ex)
                {
                    try
                    {
                        SensusException.Report("Push notification token was not set.");
                    }
                    catch (Exception)
                    {
                    }

                    // rethrow exception to give parent class an opportunity to fix the problem (e.g., by requesting a new token)
                    throw ex;
                }
                catch (Exception ex)
                {
                    try
                    {
                        SensusException.Report("Exception while updating push notification registrations:  " + ex.Message, ex);
                    }
                    catch (Exception)
                    {
                    }

                    // don't rethrow. it's not clear that a new token would fix the problem, and it could be the case that
                    // requesting a new token sets up an infinite loop of request-fail (e.g., due to a network problem or
                    // other issues in the messaging API). we have just reported the issue to the app center crash api, so hop
                }
            });
        }

        public abstract Task UpdatePushNotification(Dictionary<Tuple<string, string>, List<Protocol>> hubSasProtocols);
        public void StopProtocols()
        {
            _logger.Log("Stopping protocols.", LoggingLevel.Normal, GetType());

            foreach (var protocol in _registeredProtocols.ToArray().Where(p => p.Running))
            {
                try
                {
                    protocol.Stop();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Failed to stop protocol \"{protocol.Name}\": {ex.Message}", LoggingLevel.Normal, GetType());
                }
            }
        }

        #region Private Methods
        private void RemoveScripts(bool issueNotification, params Script[] scripts)
        {
            var removed = false;

            foreach (var script in scripts)
            {
                if (_scriptsToRun.Remove(script))
                {
                    removed = true;
                }
            }

            if (removed && issueNotification)
            {
                IssuePendingSurveysNotificationAsync(null, false);
            }
        }

        private int GetScriptIndex(Script script)
        {
            List<Script> scripts = _scriptsToRun.ToList();

            int index;

            if (scripts.Count == 0)
            {
                index = 0;
            }
            else if (scripts[scripts.Count - 1].CompareTo(script) <= 0)
            {
                index = scripts.Count;
            }
            else
            {
                index = scripts.BinarySearch(script);

                if (index < 0)
                {
                    index = ~index;
                }
            }

            return index;
        }
        #endregion
    }
}

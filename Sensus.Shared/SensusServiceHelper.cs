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

using Xamarin;
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

#if __ANDROID__ || __IOS__
using ZXing;
#endif

#if __IOS__
using XLabs.Platform.Device;
#endif

namespace Sensus
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper
    {
        #region static members
        private static SensusServiceHelper SINGLETON;
        public const int PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS = 60;
        private const string PENDING_SURVEY_NOTIFICATION_ID = "SENSUS-PENDING-SURVEY-NOTIFICATION";
        public const string XAMARIN_INSIGHTS_APP_KEY = "";
        public const string ENCRYPTION_KEY = "";

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
        public static void Initialize(Func<SensusServiceHelper> createNew, bool delay = true)
        {
            if (SINGLETON != null)
            {
                SINGLETON.Logger.Log("Serivce helper already initialized. Nothing to do.", LoggingLevel.Normal, SINGLETON.GetType());

                return;
            }

            Exception deserializeException;
            if (!TryDeserializeSingleton(out deserializeException))
            {
                // we failed to deserialize. wait a bit and try again. but don't wait too long since we're holding up the 
                // app-load sequence, which is not allowed to take too much time.
                if (delay) Thread.Sleep(5000);

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

                        string error = "Failed to construct service helper:  " + singletonCreationException.Message + Environment.NewLine + singletonCreationException.StackTrace;
                        Console.Error.WriteLine(error);
                        Exception exceptionToReport = new Exception(error);

                        try
                        {
                            Insights.Report(exceptionToReport, Insights.Severity.Error);
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
                    decryptedJSON = SensusContext.Current.Encryption.Decrypt(encryptedJsonBytes);
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

#if __IOS__ || __ANDROID__
        private ZXing.Mobile.BarcodeWriter _barcodeWriter;
#endif

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

#if __IOS__ || __ANDROID__
        [JsonIgnore]
        public ZXing.Mobile.BarcodeWriter BarcodeWriter
        {
            get
            {
                return _barcodeWriter;
            }
        }
#endif

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

        #region Constructors
        [JsonConstructor]
        protected SensusServiceHelper()
        {
            if (SINGLETON != null)
            {
                throw new SensusException("Attempted to construct new service helper when singleton already existed.");
            }

            _registeredProtocols = new ConcurrentObservableCollection<Protocol>();
            _scriptsToRun = new ConcurrentObservableCollection<Script>();
            _runningProtocolIds = new List<string>();
            _hasher = new SHA256Managed();
            _pointsOfInterest = new List<PointOfInterest>();

            // ensure that the entire QR code is always visible by using 90% the minimum screen dimension as the QR code size.
#if __ANDROID__
            int qrCodeSize = (int)(0.9 * Math.Min(XLabs.Platform.Device.Display.Metrics.WidthPixels, XLabs.Platform.Device.Display.Metrics.HeightPixels));
#elif __IOS__
            //In order for AppleDevice calls to work we need to be on the UI thread. We should always be on the made thread when creating new SensusServiceHelpers. Still, just to be safe, we're explicitly synchronizing 
            int qrCodeSize = SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() => (int)(0.9 * Math.Min(AppleDevice.CurrentDevice.Display.Height, AppleDevice.CurrentDevice.Display.Width)));
#elif LOCAL_TESTS
#else
#warning "Unrecognized platform"
#endif

#if __IOS__ || __ANDROID__
            _barcodeWriter = new ZXing.Mobile.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,

                Options = new ZXing.Common.EncodingOptions
                {
                    Height = qrCodeSize,
                    Width = qrCodeSize
                }
            };
#endif
            _flashNotificationsEnabled = true;


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

            if (!Insights.IsInitialized && string.IsNullOrWhiteSpace(XAMARIN_INSIGHTS_APP_KEY))
            {
                _logger.Log("Xamarin Insights key is empty -- not initialized.", LoggingLevel.Normal, GetType());
            }
            else if (!Insights.IsInitialized)
            {
                _logger.Log("Xamarin Insights failed to initialize.", LoggingLevel.Normal, GetType());
            }
            else
            {
                _logger.Log("Xamarin Insights sucessfully initialized.", LoggingLevel.Normal, GetType());
            }
        }
        #endregion

        public string GetHash(string s)
        {
            if (s == null)
                return null;

            StringBuilder hashBuilder = new StringBuilder();
            foreach (byte b in _hasher.ComputeHash(Encoding.UTF8.GetBytes(s)))
                hashBuilder.Append(b.ToString("x"));

            return hashBuilder.ToString();
        }

        #region platform-specific methods. this functionality cannot be implemented in a cross-platform way. it must be done separately for each platform. we are gradually migrating this functionality into the ISensusContext object.

        protected abstract void ProtectedFlashNotificationAsync(string message, bool flashLaterIfNotVisible, TimeSpan duration, Action callback);

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject, string mimeType);

        public abstract void SendEmailAsync(string toAddress, string subject, string message);

        public abstract Task TextToSpeechAsync(string text);

        public abstract Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback);

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

        #endregion

        #region add/remove running protocol ids

        public void AddRunningProtocolId(string id)
        {
            lock (_runningProtocolIds)
            {
                if (!_runningProtocolIds.Contains(id))
                    _runningProtocolIds.Add(id);

                if (_healthTestCallback == null)
                {
                    _healthTestCallback = new RepeatingCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
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

                    }, "HEALTH-TEST", null, GetType().FullName, TimeSpan.FromMilliseconds(HEALTH_TEST_DELAY_MS), TimeSpan.FromMilliseconds(HEALTH_TEST_DELAY_MS), HEALTH_TEST_REPEAT_LAG, TimeSpan.FromMinutes(1));

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
                    SensusContext.Current.CallbackScheduler.UnscheduleCallback(_healthTestCallback?.Id);
                    _healthTestCallback = null;
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
            Task.Run(() =>
            {
                Save();
                callback?.Invoke();
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
                    byte[] encryptedBytes = SensusContext.Current.Encryption.Encrypt(serviceHelperJSON);
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
            foreach (var protocol in _registeredProtocols)
            {
                if (!protocol.Running && _runningProtocolIds.Contains(protocol.Id))
                {
                    protocol.Start();
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

        public void AddScriptToRun(Script script, RunMode runMode)
        {
            var scriptsWithSameParent = _scriptsToRun.Where(s => s.SharesParentScriptWith(script)).ToArray();

            if (scriptsWithSameParent.Any() && runMode == RunMode.SingleKeepOldest)
            {
                return;
            }

            if (scriptsWithSameParent.Any() && runMode == RunMode.SingleUpdate)
            {
                foreach (var scriptWithSameParent in scriptsWithSameParent)
                {
                    _scriptsToRun.Remove(scriptWithSameParent);
                }
            }

            _scriptsToRun.Insert(0, script);
            IssuePendingSurveysNotificationAsync(script.Runner.Probe.Protocol.Id, true);
        }

        public void RemoveScript(Script script)
        {
            RemoveScripts(true, script);
        }

        public void RemoveScriptRunner(ScriptRunner runner)
        {
            RemoveScripts(true, _scriptsToRun.Where(script => script.Runner == runner).ToArray());
        }

        public void RemoveExpiredScripts(bool issueNotification)
        {
            RemoveScripts(issueNotification, _scriptsToRun.Where(s => s.Expired).ToArray());
        }

        /// <summary>
        /// Issues the pending surveys notification.
        /// </summary>
        /// <param name="protocolId">Protocol identifier used to check for alert exclusion time windows. </param>
        /// <param name="alertUser">If set to <c>true</c> alert user using sound and/or vibration.</param>
        public void IssuePendingSurveysNotificationAsync(string protocolId, bool alertUser)
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
                SensusContext.Current.Notifier.IssueNotificationAsync(pendingSurveysTitle, nextExpirationMessage, PENDING_SURVEY_NOTIFICATION_ID, protocolId, alertUser, DisplayPage.PendingSurveys);
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
            PromptForInputsAsync(windowTitle, new[] { input }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, inputs =>
           {
               callback(inputs?.First());
           });
        }

        public void PromptForInputsAsync(string windowTitle, IEnumerable<Input> inputs, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action<List<Input>> callback)
        {
            var inputGroup = new InputGroup { Name = windowTitle };

            foreach (var input in inputs)
            {
                inputGroup.Inputs.Add(input);
            }

            PromptForInputsAsync(null, new[] { inputGroup }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, null, inputGroups =>
           {
               callback(inputGroups?.SelectMany(g => g.Inputs).ToList());
           });
        }

        public void PromptForInputsAsync(DateTimeOffset? firstPromptTimestamp, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, Action postDisplayCallback, Action<IEnumerable<InputGroup>> callback)
        {
            new Thread(async () =>
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
                                try
                                {
                                    // only run the post-display callback the first time a page is displayed. the caller expects the callback
                                    // to fire only once upon first display.
                                    await voiceInput.RunAsync(firstPromptTimestamp, firstPageDisplay ? postDisplayCallback : null);
                                    firstPageDisplay = false;
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        Insights.Report(ex, Insights.Severity.Critical);
                                    }
                                    catch { }
                                }
                            }

                            responseWait.Set();
                        }
                        else
                        {
                            BringToForeground();

                            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
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
                                        // display page, which will handle setting the response wait. only animate the display for the first page.
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
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
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
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
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
                {
                    return PermissionStatus.Granted;
                }

                // the Permissions plugin requires a main activity to be present on android. ensure this below.
                BringToForeground();

                // display rationale for request to the user if needed
                if (rationale != null && await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(permission))
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        Application.Current.MainPage.DisplayAlert("Permission Request", $"On the next screen, Sensus will request access to your device's {permission.ToString().ToUpper()}. {rationale}", "OK").Wait();
                    });
                }

                // request permission from the user                    
                try
                {
                    PermissionStatus status;

                    // it's happened that the returned dictionary doesn't contain an entry for the requested permission, so check for that(https://insights.xamarin.com/app/Sensus-Production/issues/903).a
                    if (!(await CrossPermissions.Current.RequestPermissionsAsync(permission)).TryGetValue(permission, out status))
                    {
                        throw new Exception($"Permission status not returned for request:  {permission}");
                    }

                    return status;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Failed to obtain permission:  {ex.Message}", LoggingLevel.Normal, GetType());

                    return PermissionStatus.Unknown;
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
        #endregion
    }
}

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

using Newtonsoft.Json;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Xamarin.Forms;
using Sensus.Anonymization;
using System.Linq;
using System.Reflection;
using Sensus.Probes.Location;
using Sensus.UI.Inputs;
using Sensus.Probes.Apps;
using Sensus.Probes.Movement;
using System.Text;
using System.Threading.Tasks;
using Sensus.Context;
using Sensus.Probes.User.MicrosoftBand;
using Sensus.Probes.User.Scripts;
using Sensus.Callbacks;
using Sensus.Encryption;
using System.Text.RegularExpressions;
using Microsoft.AppCenter.Analytics;
using System.ComponentModel;
using Sensus.Concurrent;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon;
using Amazon.S3.Model;
using Sensus.Anonymization.Anonymizers;
using Sensus.UI;
using Sensus.Exceptions;

#if __IOS__
using HealthKit;
using Foundation;
using Sensus.iOS.Probes.User.Health;
using Plugin.Geolocator.Abstractions;
#endif

namespace Sensus
{
    /// <summary>
    /// 
    /// A Protocol defines a plan for collecting (via <see cref="Probe"/>s), anonymizing (via <see cref="Anonymization.Anonymizers.Anonymizer"/>s), and 
    /// storing (via <see cref="LocalDataStore"/>s and <see cref="RemoteDataStore"/>s) data from a device. Study organizers use Sensus to configure the 
    /// study's Protocol. Study participants use Sensus to load a Protocol and enroll in the study. All of this happens within the Sensus app.
    /// 
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
        #region static members

        public const int GPS_DEFAULT_ACCURACY_METERS = 25;
        public const int GPS_DEFAULT_MIN_TIME_DELAY_MS = 5000;
        public const int GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS = 50;
        private readonly Regex NON_ALPHANUMERIC_REGEX = new Regex("[^a-zA-Z0-9]");

        public static async Task<Protocol> CreateAsync(string name)
        {
            Protocol protocol = new Protocol(name);

            await protocol.ResetAsync(true);

            foreach (Probe probe in Probe.GetAll())
            {
                protocol.AddProbe(probe);
            }

            SensusServiceHelper.Get().RegisterProtocol(protocol);

            return protocol;
        }

        public static async Task<Protocol> DeserializeAsync(Uri webURI)
        {
            Protocol protocol = null;

            try
            {
                // check if the URI points to an S3 bucket
                AmazonS3Uri s3URI = null;
                try
                {
                    s3URI = new AmazonS3Uri(webURI);
                }
                catch (Exception)
                {
                }

                if (s3URI == null)
                {
                    TaskCompletionSource<Protocol> completionSource = new TaskCompletionSource<Protocol>();
                    WebClient downloadClient = new WebClient();

                    downloadClient.DownloadDataCompleted += async (o, e) =>
                    {
                        if (e.Error == null)
                        {
                            completionSource.SetResult(await DeserializeAsync(e.Result));
                        }
                        else
                        {
                            string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + e.Error.Message;
                            SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                            await SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                            completionSource.SetResult(null);
                        }
                    };

                    downloadClient.DownloadDataAsync(webURI);

                    protocol = await completionSource.Task;
                }
                else
                {
                    AmazonS3Client s3Client = new AmazonS3Client(SensusContext.Current.IamAccessKey, SensusContext.Current.IamAccessKeySecret, RegionEndpoint.GetBySystemName(SensusContext.Current.IamRegion));
                    GetObjectResponse response = await s3Client.GetObjectAsync(s3URI.Bucket, s3URI.Key);
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        MemoryStream byteStream = new MemoryStream();
                        response.ResponseStream.CopyTo(byteStream);
                        protocol = await DeserializeAsync(byteStream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
            }

            return protocol;
        }

        public static async Task<Protocol> DeserializeAsync(byte[] bytes)
        {
            // decrypt the bytes to JSON
            string json;
            try
            {
                json = SensusContext.Current.SymmetricEncryption.DecryptToString(bytes);
            }
            catch (Exception ex)
            {
                string message = "Failed to decrypt protocol bytes:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(message);
                return null;
            }

            // make any necessary platform conversions to type names to allow the JSON to deserialize
            try
            {
                json = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(json);
            }
            catch (Exception ex)
            {
                string message = "Failed to cross-platform convert protocol:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(message);
                return null;
            }

            // deserialize JSON to protocol object
            Protocol protocol;
            try
            {
                protocol = await DeserializeAsync(json);
            }
            catch (Exception ex)
            {
                string message = "Failed to deserialize protocol JSON:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(message);
                return null;
            }

            // check whether protocol is compatible with the current device
            if (protocol.CompatibilityMode == ProtocolCompatibilityMode.CrossPlatform ||
                protocol.CompatibilityMode == ProtocolCompatibilityMode.AndroidOnly && SensusContext.Current.Platform == Platform.Android ||
                protocol.CompatibilityMode == ProtocolCompatibilityMode.iOSOnly && SensusContext.Current.Platform == Platform.iOS)
            {
                SensusServiceHelper.Get().Logger.Log("Protocol is compatible with current device.", LoggingLevel.Normal, typeof(Protocol));
            }
            else
            {
                string message = "The study you loaded is only compatible with ";

                if (protocol.CompatibilityMode == ProtocolCompatibilityMode.AndroidOnly)
                {
                    message += "Android";
                }
                else if (protocol.CompatibilityMode == ProtocolCompatibilityMode.iOSOnly)
                {
                    message += "iOS";
                }

                message += " devices.";

                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(message);
                return null;
            }

            // set up protocol
            try
            {
                // if probes not present in the current platform are used as script triggers, we'll 
                // end up with null probes in the protocol that refer to invalid triggers. remove
                // any nulls.
                protocol.Probes.RemoveAll(probe => probe == null);

                // don't reset the protocol id -- received protocols should remain in the same study.
                await protocol.ResetAsync(false);

                // the selection index for groupable protocols comes from one of two places:  it's either the index corresponding to the 
                // protocol that was previously registered (when a groupable protocol is updated), or it's a random one (when a groupable
                // protocol is first loaded).
                int? groupableProtocolIndex = null;

                // see if we have already registered the newly deserialized protocol. when considering whether a registered
                // protocol is the match for the newly deserialized one, also check the protocols grouped with the registered
                // protocol. from the user's perspective these grouped protocols are not visible, but they should trigger
                // a match from an randomized experimental design perspective.
                Protocol registeredProtocol = null;
                foreach (Protocol p in SensusServiceHelper.Get().RegisteredProtocols)
                {
                    if (p.Equals(protocol) || p.GroupedProtocols.Contains(protocol) || protocol.GroupedProtocols.Contains(p))
                    {
                        registeredProtocol = p;
                        break;
                    }
                }

                #region if we've previously registered the protocol, the user needs to decide what to do:  either keep the previous one or use the new one
                if (registeredProtocol != null)
                {
                    await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        if (!await Application.Current.MainPage.DisplayAlert("Study Already Loaded", "The study that you just opened has already been loaded into Sensus. Would you like to use the study you just opened or continue using the previous one?", "Use the study I just opened.", "Continue using the previous study."))
                        {
                            // use the previous study
                            protocol = registeredProtocol;
                        }
                    });

                    // if the user opted to use the new protocol, we need to replace the previous one with the new one.
                    if (protocol != registeredProtocol)
                    {
                        // if the new protocol is groupable, we do not want to randomly select one out of the group. instead, we want to continue using 
                        // the same protocol that we have been using. 
                        if (protocol.GroupedProtocols.Count > 0)
                        {
                            if (protocol.Id == registeredProtocol.Id)
                            {
                                // don't re-group below. use the currently assigned protocol.
                                groupableProtocolIndex = 0;
                            }
                            else
                            {
                                // locate the index of the new protocol corresponding to the old one.
                                groupableProtocolIndex = protocol.GroupedProtocols.FindIndex(groupedProtocol => groupedProtocol.Id == registeredProtocol.Id) + 1;

                                // it's possible that the new protocol does not include the one we were previously using (e.g., if the study desiger has deleted it
                                // from the group). in this case, set the groupable index to null. we'll pick randomly below.
                                if (groupableProtocolIndex < 0)
                                {
                                    groupableProtocolIndex = null;
                                }
                            }
                        }

                        // store any data that have accumulated locally
                        await SensusServiceHelper.Get().FlashNotificationAsync("Submitting data from previous study...");
                        await registeredProtocol.LocalDataStore.WriteToRemoteAsync(CancellationToken.None);

                        // stop the study and unregister it 
                        await SensusServiceHelper.Get().FlashNotificationAsync("Stopping previous study...");
                        await registeredProtocol.StopAsync();
                        await SensusServiceHelper.Get().UnregisterProtocolAsync(registeredProtocol);
                        registeredProtocol = null;
                    }
                }
                #endregion

                // if we're not using a previously registered protocol, then we need to configure the new one.
                if (registeredProtocol == null)
                {
                    #region if grouped protocols are available, consider swapping the currently assigned one with another.
                    if (protocol.GroupedProtocols.Count > 0)
                    {
                        // if we didn't select an index above corresponding to the previously registered protocol, generated a random index.
                        if (groupableProtocolIndex == null)
                        {
                            int numProtocols = 1 + protocol.GroupedProtocols.Count;
                            groupableProtocolIndex = new Random().Next(0, numProtocols);  // inclusive min, exclusive max
                        }

                        // if protocol index == 0, then we should use the currently assigned protocol -- no action is needed. if, on 
                        // the other hand the protocol index > 0, then we need to swap in a new protocol.
                        if (groupableProtocolIndex.Value > 0)
                        {
                            int replacementIndex = groupableProtocolIndex.Value - 1;
                            Protocol replacementProtocol = protocol.GroupedProtocols[replacementIndex];

                            // rotate the configuration such that the replacement protocol has the other protocols as grouped protocols
                            replacementProtocol.GroupedProtocols.Clear();
                            replacementProtocol.GroupedProtocols.Add(protocol);
                            replacementProtocol.GroupedProtocols.AddRange(protocol.GroupedProtocols.Where(groupedProtocol => !groupedProtocol.Equals(replacementProtocol)));

                            // clear the original protocol's grouped protocols and swap in the replacement
                            protocol.GroupedProtocols.Clear();
                            protocol = replacementProtocol;
                        }
                    }
                    #endregion

                    #region add any probes for the current platform that didn't come through when deserializing.
                    // for example, android has a listening WLAN probe, but iOS has a polling WLAN probe. neither will 
                    // come through on the other platform when deserializing, since the types are not defined.
                    List<Type> deserializedProbeTypes = protocol.Probes.Select(p => p.GetType()).ToList();

                    foreach (Probe probe in Probe.GetAll())
                    {
                        if (!deserializedProbeTypes.Contains(probe.GetType()))
                        {
                            SensusServiceHelper.Get().Logger.Log("Adding missing probe to protocol:  " + probe.GetType().FullName, LoggingLevel.Normal, typeof(Protocol));
                            protocol.AddProbe(probe);
                        }
                    }

                    #endregion

                    #region remove triggers that reference unavailable probes
                    // when doing cross-platform conversions, there may be triggers that reference probes that aren't available on the
                    // current platform. remove these triggers and warn the user that the script will not run.
                    // https://insights.xamarin.com/app/Sensus-Production/issues/999
                    foreach (ScriptProbe probe in protocol.Probes.Where(probe => probe is ScriptProbe))
                    {
                        foreach (ScriptRunner scriptRunner in probe.ScriptRunners)
                        {
                            foreach (Probes.User.Scripts.Trigger trigger in scriptRunner.Triggers.ToList())
                            {
                                if (trigger.Probe == null)
                                {
                                    scriptRunner.Triggers.Remove(trigger);
                                    await SensusServiceHelper.Get().FlashNotificationAsync("Warning:  " + scriptRunner.Name + " trigger is not valid on this device.");
                                }
                            }
                        }
                    }
                    #endregion

                    SensusServiceHelper.Get().RegisterProtocol(protocol);
                }

                // protocols deserialized upon receipt (i.e., those here) are never groupable for experimental integrity reasons. we
                // do not want the user to be able to group the newly deserialized protocol with other protocols and then share the 
                // resulting grouped protocol with other participants. the user's only option is to share the protocol as-is. of course,
                // if the protocol is unlocked then the user will be able to go edit the protocol and make it groupable. this is why
                // all protocols should be locked before deployment in an experiment.
                protocol.Groupable = false;
            }
            catch (Exception ex)
            {
                string message = "Failed to set up protocol:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync(message);
                protocol = null;
            }

            return protocol;
        }

        public static async Task<Protocol> DeserializeAsync(string json)
        {
            Protocol protocol = null;

            try
            {
                // always deserialize protocols on the main thread (e.g., since a looper is required for android). also, disable
                // flash notifications so we don't get any messages that result from properties being set within the protocol.
                SensusServiceHelper.Get().FlashNotificationsEnabled = false;
                protocol = SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<Protocol>(json, SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Error while deserializing protocol from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                        return null;
                    }
                });

                if (protocol == null)
                {
                    throw new Exception("Failed to deserialize protocol from JSON.");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to deserialize protocol from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                await SensusServiceHelper.Get().FlashNotificationAsync("Failed to unpack protocol from JSON:  " + ex.Message);
            }
            finally
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = true;
            }

            return protocol;
        }

        public static async Task DisplayAndStartAsync(Protocol protocol)
        {
            if (protocol == null)
            {
                return;
            }
            else if (protocol.State == ProtocolState.Starting)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("The study \"" + protocol.Name + "\" is starting.");
            }
            else if (protocol.State == ProtocolState.Running)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("The study \"" + protocol.Name + "\" is running.");
            }
            else
            {
                await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    await protocol.StartWithUserAgreementAsync("You just opened \"" + protocol.Name + "\" within Sensus.");
                });
            }
        }

        public static async Task RunUiTestingProtocolAsync(Stream uiTestingProtocolFile)
        {
            try
            {
                // delete all current protocols -- we don't want them interfering with the one we're about to load/run.
                foreach (Protocol protocol in SensusServiceHelper.Get().RegisteredProtocols)
                {
                    await protocol.DeleteAsync();
                }

                using (MemoryStream protocolStream = new MemoryStream())
                {
                    uiTestingProtocolFile.CopyTo(protocolStream);
                    string protocolJSON = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(SensusContext.Current.SymmetricEncryption.DecryptToString(protocolStream.ToArray()));
                    Protocol protocol = await DeserializeAsync(protocolJSON);

                    if (protocol == null)
                    {
                        throw new Exception("Failed to deserialize UI testing protocol.");
                    }

                    foreach (Probe probe in protocol.Probes)
                    {
                        // UI testing is problematic with probes that take us away from Sensus, since it's difficult to automate UI 
                        // interaction outside of Sensus. disable any probes that might take us away from Sensus.

                        if (probe is FacebookProbe)
                        {
                            probe.Enabled = false;
                        }

#if __IOS__
                        if (probe is iOSHealthKitProbe)
                        {
                            probe.Enabled = false;
                        }
#endif

                        // clear the run-times collection from any script runners. need a clean start, just in case we have one-shot scripts
                        // that need to run every UI testing execution.
                        if (probe is ScriptProbe)
                        {
                            foreach (ScriptRunner scriptRunner in (probe as ScriptProbe).ScriptRunners)
                            {
                                scriptRunner.RunTimes.Clear();
                            }
                        }

                        // disable the accelerometer probe, since we use it to trigger a test script that can interrupt UI scripting.
                        if (probe is AccelerometerProbe)
                        {
                            probe.Enabled = false;
                        }
                    }

                    await DisplayAndStartAsync(protocol);
                }
            }
            catch (Exception ex)
            {
                string message = "Failed to run UI testing protocol:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                throw new Exception(message);
            }
        }

        #endregion

        public event EventHandler<ProtocolState> StateChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private string _name;
        private List<Probe> _probes;
        private ProtocolState _state;
        private ScheduledCallback _scheduledStartCallback;
        private ScheduledCallback _scheduledStopCallback;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;
        private string _storageDirectory;
        private string _lockPasswordHash;
        private AnonymizedJsonContractResolver _jsonAnonymizer;
        private DateTimeOffset _randomTimeAnchor;
        private readonly ConcurrentObservableCollection<PointOfInterest> _pointsOfInterest;
        private string _description;
        private DateTime _startTimestamp;
        private bool _startImmediately;
        private DateTime _endTimestamp;
        private bool _continueIndefinitely;
        private readonly List<Window> _alertExclusionWindows;
        private string _asymmetricEncryptionPublicKey;
        private int _participationHorizonDays;
        private string _contactEmail;
        private bool _groupable;
        private List<Protocol> _groupedProtocols;
        private float? _rewardThreshold;
        private float _gpsDesiredAccuracyMeters;
        private int _gpsMinTimeDelayMS;
        private float _gpsMinDistanceDelayMeters;
        private Dictionary<string, string> _variableValue;
        private ProtocolStartConfirmationMode _startConfirmationMode;
        private string _participantId;
        private string _pushNotificationsSharedAccessSignature;
        private string _pushNotificationsHub;
        private double _gpsLongitudeAnonymizationParticipantOffset;
        private double _gpsLongitudeAnonymizationStudyOffset;
        private bool _allowProbeDisableOnStartUp;
        private List<string> _probesSelectedOnStartUp;
        private Dictionary<Type, Probe> _typeProbe;

        // members for displaying protocol start-up
        private ProtocolStartPage _protocolStartPage;
        private bool _pushedProtocolStartPage;
        private Func<Task> _protocolStartInitiatedAsync;
        private Func<double, Task> _protocolStartAddProgressAsync;
        private Func<ProtocolState, Task> _protocolStartFinishedAsync;

        /// <summary>
        /// The study's identifier. All studies on the same device must have unique identifiers. Certain <see cref="Probe"/>s
        /// like the <see cref="Probes.Context.BluetoothDeviceProximityProbe"/> rely on the study identifiers to be the same
        /// across Android and iOS platforms in order to make detections.
        /// </summary>
        /// <value>The identifier.</value>
        [EntryStringUiProperty(null, false, 0, true)]
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Id)));
            }
        }

        /// <summary>
        /// A descriptive name for the <see cref="Protocol"/>.
        /// </summary>
        /// <value>The name.</value>
        [EntryStringUiProperty("Name:", true, 1, true)]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                FireCaptionChanged();
            }
        }

        public List<Probe> Probes
        {
            get => _probes;
            set => _probes = value;
        }

        [JsonIgnore]
        public ProtocolState State => _state;

        public LocalDataStore LocalDataStore
        {
            get => _localDataStore;
            set
            {
                if (value != _localDataStore)
                {
                    _localDataStore = value;
                    _localDataStore.Protocol = this;

                    _localDataStore.UpdatedCaptionText += (o, status) =>
                    {
                        FireSubCaptionChanged();
                    };
                }
            }
        }

        public RemoteDataStore RemoteDataStore
        {
            get => _remoteDataStore;
            set
            {
                if (value != _remoteDataStore)
                {
                    _remoteDataStore = value;
                    _remoteDataStore.Protocol = this;
                }
            }
        }

        public string StorageDirectory
        {
            get
            {
                try
                {
                    // test storage directory to ensure that it's valid
                    if (!Directory.Exists(_storageDirectory) || Directory.GetFiles(_storageDirectory).Length == -1)
                    {
                        throw new Exception("Invalid protocol storage directory.");
                    }
                }
                catch (Exception)
                {
                    // the storage directory is not valid. try resetting the storage directory.
                    try
                    {
                        ResetStorageDirectory();

                        if (!Directory.Exists(_storageDirectory) || Directory.GetFiles(_storageDirectory).Length == -1)
                        {
                            throw new Exception("Failed to reset protocol storage directory.");
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log(ex.Message, LoggingLevel.Normal, GetType());
                        throw ex;
                    }
                }

                return _storageDirectory;
            }
            set
            {
                _storageDirectory = value;

                if (!string.IsNullOrWhiteSpace(_storageDirectory) && !Directory.Exists(_storageDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(_storageDirectory);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public string LockPasswordHash
        {
            get => _lockPasswordHash;
            set => _lockPasswordHash = value;
        }

        public AnonymizedJsonContractResolver JsonAnonymizer
        {
            get => _jsonAnonymizer;
            set => _jsonAnonymizer = value;
        }

        public DateTimeOffset RandomTimeAnchor
        {
            get => _randomTimeAnchor;
            set => _randomTimeAnchor = value;
        }

        public ConcurrentObservableCollection<PointOfInterest> PointsOfInterest => _pointsOfInterest;

        /// <summary>
        /// A detailed description of the <see cref="Protocol"/> (e.g., what it does, who it is intended for, etc.).
        /// </summary>
        /// <value>The description.</value>
        [EditorUiProperty(null, true, 15, false)]
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// Whether or not to start the <see cref="Protocol"/> immediately after the user has opted into it.
        /// </summary>
        /// <value><c>true</c> to start immediately; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Start Immediately:", true, 16)]
        public bool StartImmediately
        {
            get => _startImmediately;
            set => _startImmediately = value;
        }

        /// <summary>
        /// The date on which the <see cref="Protocol"/> will start running. Only has an effect if <see cref="StartImmediately"/> 
        /// is `false`.
        /// </summary>
        /// <value>The start date.</value>
        [DateUiProperty("Start Date:", true, 17, false)]
        public DateTime StartDate
        {
            get => _startTimestamp;
            set
            {
                _startTimestamp = new DateTime(value.Year, value.Month, value.Day, _startTimestamp.Hour, _startTimestamp.Minute, _startTimestamp.Second);

                FireCaptionChanged();
            }
        }

        /// <summary>
        /// The time at which the <see cref="Protocol"/> will start running. Only has an effect if <see cref="StartImmediately"/> is `false`.
        /// </summary>
        /// <value>The start time.</value>
        [TimeUiProperty("Start Time:", true, 18, false)]
        public TimeSpan StartTime
        {
            get => _startTimestamp.TimeOfDay;
            set
            {
                _startTimestamp = new DateTime(_startTimestamp.Year, _startTimestamp.Month, _startTimestamp.Day, value.Hours, value.Minutes, value.Seconds);

                FireCaptionChanged();
            }
        }

        /// <summary>
        /// Whether or not to execute the <see cref="Protocol"/> forever after it has started.
        /// </summary>
        /// <value><c>true</c> to execute forever; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Continue Indefinitely:", true, 19)]
        public bool ContinueIndefinitely
        {
            get => _continueIndefinitely;
            set => _continueIndefinitely = value;
        }

        /// <summary>
        /// The date on which the <see cref="Protocol"/> will stop running. Only has an effect if <see cref="ContinueIndefinitely"/> is `false`.
        /// </summary>
        /// <value>The end date.</value>
        [DateUiProperty("End Date:", true, 20, false)]
        public DateTime EndDate
        {
            get => _endTimestamp;
            set => _endTimestamp = new DateTime(value.Year, value.Month, value.Day, _endTimestamp.Hour, _endTimestamp.Minute, _endTimestamp.Second);
        }

        /// <summary>
        /// The time at which the <see cref="Protocol"/> will stop running. Only has an effect if <see cref="ContinueIndefinitely"/> is `false`.
        /// </summary>
        /// <value>The end time.</value>
        [TimeUiProperty("End Time:", true, 21, false)]
        public TimeSpan EndTime
        {
            get => _endTimestamp.TimeOfDay;
            set => _endTimestamp = new DateTime(_endTimestamp.Year, _endTimestamp.Month, _endTimestamp.Day, value.Hours, value.Minutes, value.Seconds);
        }

        /// <summary>
        /// The number of days used to calculate the participation percentage. For example, if the participation horizon is
        /// 7 days, and the user has been running a <see cref="ListeningProbe"/> for 1 day, then the participation percentage
        /// would be 1/7 (~14%). On the other hand, if the participation horizon is 1 day, then the same user would have a 
        /// participation percentage of 1/1 (100%). Must be at least 1.
        /// </summary>
        /// <value>The participation horizon, in days.</value>
        [EntryIntegerUiProperty("Participation Horizon (Days):", true, 23, true)]
        public int ParticipationHorizonDays
        {
            get => _participationHorizonDays;
            set
            {
                if (value >= 1)
                {
                    _participationHorizonDays = value;
                }
            }
        }

        [JsonIgnore]
        public DateTime ParticipationHorizon => DateTime.Now.AddDays(-_participationHorizonDays);

        /// <summary>
        /// An email address for the individual who is responsible for handling questions
        /// associated with this study.
        /// </summary>
        /// <value>The contact email.</value>
        [EntryStringUiProperty("Contact Email:", true, 24, false)]
        public string ContactEmail
        {
            get => _contactEmail;
            set => _contactEmail = value;
        }

        /// <summary>
        /// Whether the user should be allowed to group the <see cref="Protocol"/> with other <see cref="Protocol"/>s to form a 
        /// bundle that participant's are randomized into.
        /// </summary>
        /// <value><c>true</c> if groupable; otherwise, <c>false</c>.</value>
        [OnOffUiProperty(null, true, 25)]
        public bool Groupable
        {
            get => _groupable;
            set => _groupable = value;
        }

        public List<Protocol> GroupedProtocols
        {
            get => _groupedProtocols;
            set => _groupedProtocols = value;
        }

        /// <summary>
        /// The participation percentage required for a user to be considered eligible for rewards.
        /// </summary>
        /// <value>The reward threshold.</value>
        [EntryFloatUiProperty("Reward Threshold:", true, 26, false)]
        public float? RewardThreshold
        {
            get => _rewardThreshold;
            set
            {
                if (value != null)
                {
                    if (value.Value < 0)
                    {
                        value = 0;
                    }
                    else if (value.Value > 1)
                    {
                        value = 1;
                    }
                }

                _rewardThreshold = value;
            }
        }

        [JsonIgnore]
        public double Participation
        {
            get
            {
                double[] participations = _probes.Select(probe => probe.GetParticipation())
                                                 .Where(participation => participation != null)
                                                 .Select(participation => participation.GetValueOrDefault())
                                                 .ToArray();

                // there will not be any participations if all probes are disabled -- perfect participation by definition
                if (participations.Length == 0)
                {
                    return 1;
                }
                else
                {
                    return participations.Average();
                }
            }
        }

        /// <summary>
        /// The desired accuracy in meters of the collected GPS readings. There are no guarantees that this accuracy
        /// will be achieved.
        /// </summary>
        /// <value>The GPS desired accuracy, in meters.</value>
        [EntryFloatUiProperty("GPS - Desired Accuracy (Meters):", true, 27, true)]
        public float GpsDesiredAccuracyMeters
        {
            get => _gpsDesiredAccuracyMeters;
            set
            {
                if (value <= 0)
                {
                    value = GPS_DEFAULT_ACCURACY_METERS;
                }

                _gpsDesiredAccuracyMeters = value;
            }
        }

        /// <summary>
        /// The minimum amount of time in milliseconds to wait between deliveries of GPS readings.
        /// </summary>
        /// <value>The GPS minimum time delay, in milliseconds.</value>
        [EntryIntegerUiProperty("GPS - Minimum Time Delay (MS):", true, 28, true)]
        public int GpsMinTimeDelayMS
        {
            get => _gpsMinTimeDelayMS;
            set
            {
                if (value < 0)
                {
                    value = GPS_DEFAULT_MIN_TIME_DELAY_MS;
                }

                _gpsMinTimeDelayMS = value;
            }
        }

        /// <summary>
        /// The minimum distance in meters to wait between deliveries of GPS readings.
        /// </summary>
        /// <value>The GPS minimum distance delay, in meters.</value>
        [EntryFloatUiProperty("GPS - Minimum Distance Delay (Meters):", true, 29, true)]
        public float GpsMinDistanceDelayMeters
        {
            get => _gpsMinDistanceDelayMeters;
            set
            {
                if (value < 0)
                {
                    value = GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS;
                }

                _gpsMinDistanceDelayMeters = value;
            }
        }

        public Dictionary<string, string> VariableValue
        {
            get => _variableValue;
            set => _variableValue = value;
        }

        /// <summary>
        /// A <see cref="Protocol"/> may delare variables whose values can be easily reused throughout the
        /// system. For example, if many of the survey inputs share a particular substring (e.g., the study 
        /// name), consider defining a variable named `study-name` that holds the study name. You can then
        /// reference this variable when defining the survey input label via `{study-name}`. The format
        /// of this field is `variable-name:variable-value`.
        /// </summary>
        /// <value>The variable value user interface property.</value>
        [EditableListUiProperty("Variables:", true, 30, false)]
        [JsonIgnore]
        public List<string> VariableValueUiProperty
        {
            get => _variableValue.Select(kvp => kvp.Key + ": " + kvp.Value).ToList();
            set
            {
                _variableValue = new Dictionary<string, string>();

                if (value != null)
                {
                    foreach (string variableValueStr in value)
                    {
                        int colonIndex = variableValueStr.IndexOf(':');

                        // if there is no colon, use the entire string as the variable
                        if (colonIndex < 0)
                        {
                            colonIndex = variableValueStr.Length;
                        }

                        // get the variable, ignoring non-alphanumeric characters
                        string variable = NON_ALPHANUMERIC_REGEX.Replace(variableValueStr.Substring(0, colonIndex), "");
                        if (!string.IsNullOrWhiteSpace(variable))
                        {
                            // get the value, which is anything after the colon
                            string variableValue = null;
                            if (colonIndex < variableValueStr.Length - 1)
                            {
                                variableValue = variableValueStr.Substring(colonIndex + 1).Trim();

                                // if the variable value is empty then set it to null
                                if (string.IsNullOrWhiteSpace(variableValue))
                                {
                                    variableValue = null;
                                }
                            }

                            _variableValue[variable] = variableValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the participant identifier.
        /// </summary>
        /// <value>The participant identifier.</value>
        public string ParticipantId
        {
            get => _participantId;
            set => _participantId = value;
        }

        #region iOS-specific protocol properties

#if __IOS__
        [OnOffUiProperty("GPS - Pause Location Updates:", true, 30)]
        public bool GpsPauseLocationUpdatesAutomatically { get; set; } = false;

        [ListUiProperty("GPS - Pause Activity Type:", true, 31, new object[] { ActivityType.Other, ActivityType.AutomotiveNavigation, ActivityType.Fitness, ActivityType.OtherNavigation }, false)]
        public ActivityType GpsPauseActivityType { get; set; } = ActivityType.Other;

        [OnOffUiProperty("GPS - Significant Changes:", true, 32)]
        public bool GpsListenForSignificantChanges { get; set; } = false;

        [OnOffUiProperty("GPS - Defer Location Updates:", true, 33)]
        public bool GpsDeferLocationUpdates { get; set; } = false;

        private float _gpsDeferralDistanceMeters = 500;

        [EntryFloatUiProperty("GPS - Deferral Distance (Meters):", true, 34, false)]
        public float GpsDeferralDistanceMeters
        {
            get => _gpsDeferralDistanceMeters;
            set
            {
                if (value < 0)
                {
                    value = -1;
                }

                _gpsDeferralDistanceMeters = value;
            }
        }

        private float _gpsDeferralTimeMinutes = 5;

        [EntryFloatUiProperty("GPS - Deferral Time (Mins.):", true, 35, false)]
        public float GpsDeferralTimeMinutes
        {
            get => _gpsDeferralTimeMinutes;
            set
            {
                if (value < 0)
                {
                    value = -1;
                }

                _gpsDeferralTimeMinutes = value;
            }
        }
#endif

        /// <summary>
        /// A comma-separated list of time windows during which alerts from Sensus (e.g., notifications
        /// about new surveys) should not have a sound or vibration associated with them. The format
        /// is the same as described for <see cref="ScriptRunner.TriggerWindowsString"/>, except that 
        /// exact times (e.g., 11:32am) do not make any sense -- only windows (e.g., 11:32am-1:00pm) do.
        /// The start time must precede the end time (e.g., 19:00-2:00 is not permitted). To specify,
        /// such a time, provide both intervals (e.g.:  0:00-2:00,19:00-23:59).
        /// </summary>
        /// <value>The alert exclusion window string.</value>
        [EntryStringUiProperty("Alert Exclusion Windows:", true, 36, false)]
        public string AlertExclusionWindowString
        {
            get
            {
                lock (_alertExclusionWindows)
                {
                    return string.Join(", ", _alertExclusionWindows);
                }
            }
            set
            {
                if (value == AlertExclusionWindowString)
                {
                    return;
                }

                lock (_alertExclusionWindows)
                {
                    _alertExclusionWindows.Clear();

                    try
                    {
                        _alertExclusionWindows.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(windowString => new Window(windowString)));
                    }
                    catch
                    {
                        // ignore improperly formatted trigger windows
                    }

                    _alertExclusionWindows.Sort();
                }
            }
        }

        /// <summary>
        /// Sensus is able to use asymmetric key encryption to secure data before transmission from the device to a remote endpoint (e.g., AWS S3). This 
        /// provides a layer of security on top of SSL encryption and certificate pinning. For example, even if an attacker is able to intercept 
        /// and decrypt a service request (e.g., write data) to AWS S3 via a man-in-the-middle attack, the attacker would not be able to decrypt 
        /// the Sensus data payload, which is encrypted with an additional public/private key pair that you control. This protects against two 
        /// threats. First, it protects against the case where a man-in-the-middle has gained access to your pinned private encryption key and 
        /// intercepts data. Second, it protects against unauthorized access to Sensus data payloads after storage within the intended system 
        /// (e.g., within AWS S3). In the latter case, the data payloads are transferred to the correct server, but they live unencrypted on 
        /// that system. Asymmetric encryption prevents unauthorized access to the data by ensuring that Sensus data payloads can only be decrypted 
        /// by those who have the asymmetric private encryption key. To use asymmetric data encryption within Sensus, you must generate a public/private 
        /// key pair and enter the public key within <see cref="AsymmetricEncryptionPublicKey"/>. You can generate a public/private key pair in the 
        /// appropriate format using the following steps (on Mac):
        /// 
        ///   * Generate a 2048-bit `RSA PRIVATE KEY`: 
        ///  
        ///     ```
        ///     openssl genrsa -des3 -out private.pem 2048
        ///     ```
        /// 
        ///   * Extract the `PUBLIC KEY` for entering into your Sensus <see cref="Protocol"/>:
        /// 
        ///     ```
        ///     openssl rsa -in private.pem -outform PEM -pubout -out public.pem
        ///     ```
        /// 
        ///   * Use the `PUBLIC KEY` contained within public.pem as <see cref="AsymmetricEncryptionPublicKey"/>.
        /// 
        /// Keep all `PRIVATE KEY` information safe and secure. Never share it.
        /// 
        /// </summary>
        /// <value>The asymmetric encryption public key.</value>
        [EntryStringUiProperty("Asymmetric Encryption Public Key:", true, 37, false)]
        public string AsymmetricEncryptionPublicKey
        {
            get => _asymmetricEncryptionPublicKey;
            set => _asymmetricEncryptionPublicKey = value?.Trim().Replace("\n", "").Replace(" ", "");
        }

        [JsonIgnore]
        public AsymmetricEncryption AsymmetricEncryption => new AsymmetricEncryption(_asymmetricEncryptionPublicKey);

        #endregion

        /// <summary>
        /// Whether or not to allow the user to view data being collected by the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow View Data:", true, 38)]
        public bool AllowViewData { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to view the status of the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow View Status:", true, 39)]
        public bool AllowViewStatus { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to manually submit data being collected by the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Submit Data:", true, 40)]
        public bool AllowSubmitData { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to display/scan participation QR codes for the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Participation Scanning:", true, 41)]
        public bool AllowParticipationScanning { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to copy the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Copy:", true, 42)]
        public bool AllowCopy { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to share the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Protocol Share:", true, 43)]
        public bool Shareable { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to share local data collected on the device.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Local Data Share:", true, 44)]
        public bool AllowLocalDataShare { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to reset their participant ID. See <see cref="Protocol.ParticipantId"/> and <see cref="Protocol.StartConfirmationMode"/> for more information.
        /// </summary>
        /// <value><c>true</c> if allow participant identifier reset; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow ID Reset:", true, 45)]
        public bool AllowParticipantIdReset { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to enter tagging mode. See [this article](xref:tagging_mode) for more information.
        /// </summary>
        /// <value><c>true</c> if allow tagging; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Tagging:", true, 46)]
        public bool AllowTagging { get; set; } = false;

        /// <summary>
        /// The tags to make available when in tagging mode. See [this article](xref:tagging_mode) for more information.
        /// </summary>
        /// <value>The tags.</value>
        [EditableListUiProperty("Available Tags:", true, 47, false)]
        public List<string> AvailableTags { get; set; } = new List<string>();

        /// <summary>
        /// The current event identifier for tagging. See [this article](xref:tagging_mode) for more information.
        /// </summary>
        /// <value>The tag identifier.</value>
        public string TaggedEventId { get; set; }

        /// <summary>
        /// The current tags applied during event tagging. See [this article](xref:tagging_mode) for more information.
        /// </summary>
        /// <value>The set tags.</value>
        public List<string> TaggedEventTags { get; set; } = new List<string>();

        /// <summary>
        /// The time at which the current tagging started.
        /// </summary>
        /// <value>The tagging start timestamp.</value>
        public DateTimeOffset? TaggingStartTimestamp { get; set; }

        /// <summary>
        /// The time at which the current tagging ended.
        /// </summary>
        /// <value>The tagging end timestamp.</value>
        public DateTimeOffset? TaggingEndTimestamp { get; set; }

        /// <summary>
        /// A list of taggings to export.
        /// </summary>
        /// <value>The taggings to export.</value>
        public List<string> TaggingsToExport { get; } = new List<string>();

        /// <summary>
        /// The user can be asked to confirm starting the <see cref="Protocol"/> in serveral ways. See <see cref="ProtocolStartConfirmationMode"/>
        /// for more information.
        /// </summary>
        /// <value>The protocol start confirmation mode.</value>
        [ListUiProperty("Start Confirmation Mode:", true, 48, new object[] { ProtocolStartConfirmationMode.None, ProtocolStartConfirmationMode.RandomDigits, ProtocolStartConfirmationMode.ParticipantIdDigits, ProtocolStartConfirmationMode.ParticipantIdText, ProtocolStartConfirmationMode.ParticipantIdQrCode }, true)]
        public ProtocolStartConfirmationMode StartConfirmationMode
        {
            get => _startConfirmationMode;
            set => _startConfirmationMode = value;
        }

        /// <summary>
        /// The push notification hub to listen to. This can be created within the Azure Portal. The
        /// value to use here is the name of the hub (e.g., xxxx-notifications). You must also include
        /// the <see cref="PushNotificationsSharedAccessSignature"/> for this hub.
        /// </summary>
        /// <value>The push notifications hub.</value>
        [EntryStringUiProperty("Push Notification Hub:", true, 49, false)]
        public string PushNotificationsHub
        {
            get => _pushNotificationsHub;
            set => _pushNotificationsHub = value;
        }

        /// <summary>
        /// The shared access signature for listening for push notifications at the <see cref="PushNotificationsHub"/>. This
        /// value can be obtained by inspecting the Access Policies tab of the Notification Hub within the Azure Portal. Locate
        /// the DefaultListenSharedAccessSignature policy and copy the entire value of the connection string into this field.
        /// </summary>
        /// <value>The push notifications shared access signature.</value>
        [EntryStringUiProperty("Push Notifications Shared Access Signature:", true, 50, false)]
        public string PushNotificationsSharedAccessSignature
        {
            get => _pushNotificationsSharedAccessSignature;
            set => _pushNotificationsSharedAccessSignature = value;
        }

        /// <summary>
        /// Specifies whether the current <see cref="Protocol"/> should be compatible with Android only, iOS only, or both.
        /// </summary>
        /// <value>The protocol compatibility mode.</value>
        [ListUiProperty("Compatibility:", true, 51, new object[] { ProtocolCompatibilityMode.CrossPlatform, ProtocolCompatibilityMode.AndroidOnly, ProtocolCompatibilityMode.iOSOnly }, true)]
        public ProtocolCompatibilityMode CompatibilityMode { get; set; } = ProtocolCompatibilityMode.CrossPlatform;

        /// <summary>
        /// Sets wether to allow the user to disable specific protocol probes on start up
        /// </summary>
        /// <value>Allow user to disable probes on start up</value>
        [OnOffUiProperty("Allow Probe Disable on startup:", true, 52)]
        public bool AllowProbeDisableOnStartUp
        {
            get => _allowProbeDisableOnStartUp;
            set => _allowProbeDisableOnStartUp = value;
        }



        /// <summary>
        /// We regenerate the offset every time a protocol starts, so there's 
        /// no need to serialize it. Furthermore, we never want the offset
        /// to be shared.
        /// </summary>
        /// <value>The gps longitude anonymization participant offset.</value>
        [JsonIgnore]
        public double GpsLongitudeAnonymizationParticipantOffset
        {
            get => _gpsLongitudeAnonymizationParticipantOffset;
            set => _gpsLongitudeAnonymizationParticipantOffset = value;
        }

        public double GpsLongitudeAnonymizationStudyOffset
        {
            get => _gpsLongitudeAnonymizationStudyOffset;
            set => _gpsLongitudeAnonymizationStudyOffset = value;
        }

        [JsonIgnore]
        public bool StartIsScheduled
        {
            get
            {
                if (_scheduledStartCallback != null &&
                    SensusContext.Current.CallbackScheduler.ContainsCallback(_scheduledStartCallback) &&
                    _scheduledStartCallback.NextExecution.HasValue &&
                    _scheduledStartCallback.NextExecution.Value > DateTime.Now)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a <see cref="Random"/> that is seeded specifically to the participant.
        /// </summary>
        /// <value>The seeded <see cref="Random"/>.</value>
        [JsonIgnore]
        public Random LongitudeOffsetParticipantSeededRandom
        {
            get
            {
                // seed the user-level GPS origin based on participant or device ID, preferring the former.
                Random random;
                if (!string.IsNullOrWhiteSpace(_participantId))
                {
                    random = new Random(_participantId.GetHashCode());
                }
                else
                {
                    random = new Random(SensusServiceHelper.Get().DeviceId.GetHashCode());
                }

                return random;
            }
        }

        [JsonIgnore]
        public string Caption
        {
            get
            {
                string caption = _name + " (";

                if (StartIsScheduled)
                {
                    caption += "Scheduled:  " + StartDate.ToShortDateString() + " " + (StartDate.Date + StartTime).ToShortTimeString();
                }
                else
                {
                    caption += _state;
                }

                return caption + ")";
            }
        }

        [JsonIgnore]
        public string SubCaption => _localDataStore?.CaptionText;

        /// <summary>
        /// For JSON deserialization
        /// </summary>
        private Protocol()
        {
            _state = ProtocolState.Stopped;
            _lockPasswordHash = "";
            _jsonAnonymizer = new AnonymizedJsonContractResolver(this);
            _pointsOfInterest = new ConcurrentObservableCollection<PointOfInterest>();
            _participationHorizonDays = 1;
            _alertExclusionWindows = new List<Window>();
            _asymmetricEncryptionPublicKey = null;
            _startTimestamp = DateTime.Now;
            _endTimestamp = DateTime.Now;
            _startImmediately = true;
            _continueIndefinitely = true;
            _groupable = false;
            _groupedProtocols = new List<Protocol>();
            _rewardThreshold = null;
            _gpsDesiredAccuracyMeters = GPS_DEFAULT_ACCURACY_METERS;
            _gpsMinTimeDelayMS = GPS_DEFAULT_MIN_TIME_DELAY_MS;
            _gpsMinDistanceDelayMeters = GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS;
            _variableValue = new Dictionary<string, string>();
            _startConfirmationMode = ProtocolStartConfirmationMode.None;
            _allowProbeDisableOnStartUp = false;
            _probes = new List<Probe>();
        }

        private Protocol(string name) : this()
        {
            _name = name;
        }

        private void AddProbe(Probe probe)
        {
            probe.Protocol = this;

            // since the new probe was just bound to this protocol, we need to let this protocol know about this probe's default anonymization preferences.
            foreach (PropertyInfo anonymizableProperty in probe.DatumType.GetProperties().Where(property => property.GetCustomAttribute<Anonymizable>() != null))
            {
                Anonymizable anonymizableAttribute = anonymizableProperty.GetCustomAttribute<Anonymizable>(true);
                _jsonAnonymizer.SetAnonymizer(anonymizableProperty, anonymizableAttribute.DefaultAnonymizer);
            }

            _probes.Add(probe);
            _probes.Sort(new Comparison<Probe>((p1, p2) => p1.DisplayName.CompareTo(p2.DisplayName)));
        }

        public bool TryGetProbe(Type type, out Probe probe)
        {
            lock (this)
            {
                if (_typeProbe == null)
                {
                    _typeProbe = new Dictionary<Type, Probe>();

                    foreach (Probe p in _probes)
                    {
                        _typeProbe.Add(p.GetType(), p);
                    }
                }

                return _typeProbe.TryGetValue(type, out probe);
            }
        }

        private async Task ResetAsync(bool resetId)
        {
            Random random = new Random();

            // reset id and storage directory (directory might exist if deserializing the same protocol multiple times)
            if (resetId)
            {
                _id = Guid.NewGuid().ToString();

                // if this is a new study (indicated by resetting the ID), randomly initialize GPS longitude offset.
                _gpsLongitudeAnonymizationStudyOffset = LongitudeOffsetGpsAnonymizer.GetOffset(random);
            }

            // nobody else should receive the participant ID or participant anonymization offset
            _participantId = null;
            _gpsLongitudeAnonymizationParticipantOffset = 0;

            // reset local storage
            ResetStorageDirectory();

            // pick a random time anchor within the first 1000 years AD. we got a strange exception in insights about the resulting datetime having a year
            // outside of [0,10000]. no clue how this could happen, but we'll guard against it all the same. we do this regardless of whether we're 
            // resetting the protocol ID, as everyone should have a different anchor. in the future, perhaps we'll do something similar to what we do for GPS.
            try
            {
                _randomTimeAnchor = new DateTimeOffset((long)(random.NextDouble() * new DateTimeOffset(1000, 1, 1, 0, 0, 0, new TimeSpan()).Ticks), new TimeSpan());
            }
            catch (Exception) { }

            // reset probes
            foreach (Probe probe in _probes)
            {
                await probe.ResetAsync();

                // reset enabled status of probes to the original values. probes can be disabled when the protocol is started (e.g., if the user cancels out of facebook login.)
                probe.Enabled = probe.OriginallyEnabled;

                // if we reset the protocol id, assign new group and input ids to all scripts
                if (probe is ScriptProbe && resetId)
                {
                    foreach (ScriptRunner runner in (probe as ScriptProbe).ScriptRunners)
                    {
                        foreach (InputGroup inputGroup in runner.Script.InputGroups)
                        {
                            inputGroup.Id = Guid.NewGuid().ToString();

                            foreach (Input input in inputGroup.Inputs)
                            {
                                input.GroupId = inputGroup.Id;
                                input.Id = Guid.NewGuid().ToString();
                            }
                        }
                    }
                }
            }

            if (_localDataStore != null)
            {
                _localDataStore.Reset();
            }

            if (_remoteDataStore != null)
            {
                _remoteDataStore.Reset();
            }
        }

        private void ResetStorageDirectory()
        {
            StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), _id);
        }

        public void Save(string path)
        {
            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] encryptedBytes = SensusContext.Current.SymmetricEncryption.Encrypt(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS));
                file.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }

        public async Task<Protocol> CopyAsync(bool resetId, bool register)
        {
            Protocol protocol = await DeserializeAsync(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS));

            await protocol.ResetAsync(resetId);

            if (register)
            {
                SensusServiceHelper.Get().RegisterProtocol(protocol);
            }

            return protocol;
        }

        public async Task ShareAsync()
        {
            // make a deep copy of this protocol so we can reset it for sharing. don't reset the id of the 
            // protocol to keep it in the same study. also do not register the copy since we're just going 
            // to send it off rather than show it in the UI.
            Protocol protocolCopy = await CopyAsync(false, false);

            // write protocol to file and share
            string sharePath = SensusServiceHelper.Get().GetSharePath(".json");
            protocolCopy.Save(sharePath);
            await SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Protocol:  " + protocolCopy.Name, "application/json");
        }

        private async Task PrivateStartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // prevent concurrent calls to the current method
                lock (this)
                {
                    if (_state == ProtocolState.Stopped)
                    {
                        _state = ProtocolState.Starting;
                        FireStateChanged();
                    }
                    else
                    {
                        return;
                    }
                }

                await (_protocolStartInitiatedAsync?.Invoke() ?? Task.CompletedTask);

                // generate the participant-specific longitude offset. as long as the participant identifier does not change, neither will this offset.
                _gpsLongitudeAnonymizationParticipantOffset = LongitudeOffsetGpsAnonymizer.GetOffset(LongitudeOffsetParticipantSeededRandom);

                _scheduledStartCallback = null;
                FireCaptionChanged();

                // there are five steps to starting a protocol
                //
                // 1) local data store
                // 2) remote data store
                // 3) probes
                // 4) push notification registrations
                // 5) saving the protocol
                //
                // give each step 20 percent
                double perStepPercent = 0.2;

                // track any exception that can cancel the start
                Exception cancelStartException = null;

                // start local data store
                try
                {
                    if (_localDataStore == null)
                    {
                        throw new Exception("Local data store not defined.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    await _localDataStore.StartAsync();
                }
                catch (Exception ex)
                {
                    cancelStartException = ex;
                }

                await (_protocolStartAddProgressAsync?.Invoke(perStepPercent) ?? Task.CompletedTask);

                // start remote data store
                if (cancelStartException == null)
                {
                    try
                    {
                        if (_remoteDataStore == null)
                        {
                            throw new Exception("Remote data store not defined.");
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        await _remoteDataStore.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        cancelStartException = ex;
                    }
                }

                await (_protocolStartAddProgressAsync?.Invoke(perStepPercent) ?? Task.CompletedTask);

                // start probes
                if (cancelStartException == null)
                {
                    try
                    {
                        // if we're on iOS, gather up all of the health-kit probes so that we can request their permissions in one batch
#if __IOS__
                        if (HKHealthStore.IsHealthDataAvailable)
                        {
                            List<iOSHealthKitProbe> enabledHealthKitProbes = new List<iOSHealthKitProbe>();
                            foreach (Probe probe in _probes)
                            {
                                if (probe.Enabled && probe is iOSHealthKitProbe)
                                {
                                    enabledHealthKitProbes.Add(probe as iOSHealthKitProbe);
                                }
                            }

                            if (enabledHealthKitProbes.Count > 0)
                            {
                                NSSet objectTypesToRead = NSSet.MakeNSObjectSet<HKObjectType>(enabledHealthKitProbes.Select(probe => probe.ObjectType).Distinct().ToArray());
                                HKHealthStore healthStore = new HKHealthStore();
                                Tuple<bool, NSError> successError = await healthStore.RequestAuthorizationToShareAsync(new NSSet(), objectTypesToRead);

                                if (successError.Item2 != null)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Error while requesting HealthKit authorization:  " + successError.Item2.Description, LoggingLevel.Normal, GetType());
                                }
                            }
                        }
#endif

                        SensusServiceHelper.Get().Logger.Log("Starting probes for protocol " + _name + ".", LoggingLevel.Normal, GetType());
                        int probesEnabled = 0;
                        bool startMicrosoftBandProbes = true;
                        int numProbesToStart = _probes.Count(p => p.Enabled && p.Selected);
                        double perProbeStartProgressPercent = perStepPercent / numProbesToStart;
                        foreach (Probe probe in _probes)
                        {
                            if (probe.Enabled && probe.Selected == true)
                            {
                                if (probe is MicrosoftBandProbeBase && !startMicrosoftBandProbes)
                                {
                                    await (_protocolStartAddProgressAsync?.Invoke(perProbeStartProgressPercent) ?? Task.CompletedTask);
                                    continue;
                                }

                                cancellationToken.ThrowIfCancellationRequested();

                                try
                                {
                                    await probe.StartAsync();
                                }
                                catch (MicrosoftBandClientConnectException)
                                {
                                    // if we failed to start a microsoft band probe due to a client connect exception, don't attempt to start the other
                                    // band probes. instead, rely on the band health check to periodically attempt to connect to the band. if and when this
                                    // succeeds, all band probes will then be started.
                                    startMicrosoftBandProbes = false;
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Exception while starting probe:  " + ex.Message, LoggingLevel.Normal, GetType());
                                }

                                // probe might become disabled during Start due to a NotSupportedException
                                if (probe.Enabled)
                                {
                                    ++probesEnabled;
                                }

                                await (_protocolStartAddProgressAsync?.Invoke(perProbeStartProgressPercent) ?? Task.CompletedTask);
                            }
                        }

                        if (probesEnabled == 0)
                        {
                            throw new Exception("No probes were enabled.");
                        }
                    }
                    catch (OperationCanceledException cancellationException)
                    {
                        cancelStartException = cancellationException;
                    }
                    catch (Exception probeException)
                    {
                        // don't stop the protocol if we get an exception while starting probes. we might recover 
                        // from a failed probe (e.g., permission denied).
                        string message = "Failure while starting probes:  " + probeException.Message;
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        await SensusServiceHelper.Get().FlashNotificationAsync(message);
                    }
                }

                // register with push notification hubs
                if (cancelStartException == null)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await SensusServiceHelper.Get().UpdatePushNotificationRegistrationsAsync(cancellationToken);

                        await (_protocolStartAddProgressAsync?.Invoke(perStepPercent) ?? Task.CompletedTask);
                    }
                    catch (OperationCanceledException cancellationException)
                    {
                        cancelStartException = cancellationException;
                    }
                    catch (Exception registrationException)
                    {
                        // don't stop the protocol if we weren't able to update push notification registrations. we might recover.
                        SensusServiceHelper.Get().Logger.Log("Exception while updating push notification registrations:  " + registrationException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // add running protocol, which starts the health test callback.
                if (cancelStartException == null)
                {
                    try
                    {
                        await SensusServiceHelper.Get().AddRunningProtocolIdAsync(_id);
                    }
                    catch (Exception addRunningProtocolException)
                    {
                        // don't stop the protocol if we weren't able to add. we might recover.
                        SensusServiceHelper.Get().Logger.Log("Exception while adding running protocol:  " + addRunningProtocolException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // save app state
                if (cancelStartException == null)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // save the state of the app in case it crashes or -- on ios -- in case the user terminates
                        // the app by swiping it away. once saved, we'll start back up properly if/when the app restarts
                        await SensusServiceHelper.Get().SaveAsync();

                        await (_protocolStartAddProgressAsync?.Invoke(perStepPercent) ?? Task.CompletedTask);
                    }
                    catch (OperationCanceledException cancellationException)
                    {
                        cancelStartException = cancellationException;
                    }
                    catch (Exception saveException)
                    {
                        // don't stop the protocol if we weren't able to save. we might recover.
                        SensusServiceHelper.Get().Logger.Log("Failure while saving app state:  " + saveException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // wrap up if there was no start-cancelling exception
                if (cancelStartException == null)
                {
                    _state = ProtocolState.Running;
                    FireStateChanged();

                    await SensusServiceHelper.Get().FlashNotificationAsync("Started \"" + _name + "\".");
                }
                else
                {
                    throw new OperationCanceledException("Start was cancelled.", cancelStartException);
                }
            }
            catch (Exception startException)
            {
                // the current method must leave the protocol in a suitable state, either stopped or running. if 
                // we're in anything these states, then stop the protocol.
                if (_state != ProtocolState.Running)
                {
                    string message = "Error while starting study:  " + startException.Message;
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                    await SensusServiceHelper.Get().FlashNotificationAsync(message);

                    // stop the study
                    try
                    {
                        await StopAsync();
                    }
                    catch (Exception stopException)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while stopping study after failing to start it:  " + stopException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // report any non-cancellation exceptions. we should not see them.
                if (!(startException is OperationCanceledException))
                {
                    SensusException.Report("Non-cancellation exception while starting protocol:  " + startException.Message, startException);
                }
            }

            await (_protocolStartFinishedAsync?.Invoke(_state) ?? Task.CompletedTask);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_startImmediately || (DateTime.Now > _startTimestamp))
            {
                await PrivateStartAsync(cancellationToken);
            }
            else
            {
                await ScheduleStartAsync();
            }

            if (!_continueIndefinitely)
            {
                await ScheduleStopAsync();
            }
        }

        public async Task ScheduleStartAsync()
        {
            TimeSpan timeUntilStart = _startTimestamp - DateTime.Now;

            _scheduledStartCallback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                await PrivateStartAsync(cancellationToken);
                _scheduledStartCallback = null;

            }, timeUntilStart, "START", _id, this, null,
#if __ANDROID__
            $"Started study: {Name}.");
#elif __IOS__
            $"Please open to start study {Name}.");
#else
            $"Started study: {Name}.");
#endif

            await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_scheduledStartCallback);

            // add the token to the backend, as the push notification for the schedule start needs to be active.
            await SensusServiceHelper.Get().UpdatePushNotificationRegistrationsAsync(default(CancellationToken));

            FireCaptionChanged();
        }

        public async Task CancelScheduledStartAsync()
        {
            await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(_scheduledStartCallback);
            _scheduledStartCallback = null;

            // remove the token to the backend, as the push notification for the schedule start needs to be deactivated.
            await SensusServiceHelper.Get().UpdatePushNotificationRegistrationsAsync(default(CancellationToken));

            FireCaptionChanged();

            // we might have scheduled a stop when starting the protocol, so be sure to cancel it.
            await CancelScheduledStopAsync();
        }

        public async Task ScheduleStopAsync()
        {
            TimeSpan timeUntilStop = _endTimestamp - DateTime.Now;

            _scheduledStopCallback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                await StopAsync();
                _scheduledStopCallback = null;

            }, timeUntilStop, "STOP", _id, this, null,
#if __ANDROID__
            $"Stopped study: {Name}.");
#elif __IOS__
            $"Please open to stop study: {Name}.");
#else
            $"Stopped study: {Name}.");
#endif

            await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_scheduledStopCallback);
        }

        public async Task CancelScheduledStopAsync()
        {
            await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(_scheduledStopCallback);
            _scheduledStopCallback = null;
        }

        /// <summary>
        /// Starts the current <see cref="Protocol"/> after displaying a message to the user indicating what is about to happen. This is
        /// also the place where the user's agreement to the <see cref="Protocol"/> is obtained through the various 
        /// <see cref="ProtocolStartConfirmationMode"/> options. After obtaining agreement, a <see cref="UI.ProtocolStartPage"/> is displayed
        /// to show progress and prevent the user from interacting with the app until the <see cref="Protocol"/> is fully started.
        /// </summary>
        /// <returns>The with user agreement async.</returns>
        /// <param name="startupMessage">Startup message to display, along with the standard information.</param>
        public async Task StartWithUserAgreementAsync(string startupMessage = null)
        {
            if (!_probes.Any(probe => probe.Enabled))
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("Probes not enabled. Cannot start.");
                return;
            }

            if (!_continueIndefinitely && _endTimestamp <= DateTime.Now)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("You cannot start this study because it has already ended.");
                return;
            }

            List<Input> inputs = new List<Input>();

            if (!string.IsNullOrWhiteSpace(startupMessage))
            {
                inputs.Add(new LabelOnlyInput(startupMessage));
            }

            if (!string.IsNullOrWhiteSpace(_description))
            {
                inputs.Add(new LabelOnlyInput(_description));
            }

            inputs.Add(new LabelOnlyInput("This study will start " + (_startImmediately || DateTime.Now >= _startTimestamp ? "immediately" : "on " + _startTimestamp.ToShortDateString() + " at " + _startTimestamp.ToShortTimeString()) +
                                          " and " + (_continueIndefinitely ? "continue indefinitely." : "stop on " + _endTimestamp.ToShortDateString() + " at " + _endTimestamp.ToShortTimeString() + ".") +
                                          " The following data will be collected:"));

            var enabledProbes = _probes.Where(w => w.Enabled && w.StoreData).OrderBy(o => o.DisplayName).ToList();
            int collectionDescriptionFontSize = 15;

            if (AllowProbeDisableOnStartUp == false)
            {
                StringBuilder collectionDescription = new StringBuilder();
                foreach (Probe probe in enabledProbes)
                {
                    probe.Selected = true;
                    string probeCollectionDescription = probe.CollectionDescription;
                    if (!string.IsNullOrWhiteSpace(probeCollectionDescription))
                    {
                        collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + probeCollectionDescription);
                    }
                }

                LabelOnlyInput collectionDescriptionLabel = null;
                if (collectionDescription.Length == 0)
                {
                    collectionDescriptionLabel = new LabelOnlyInput("No information will be collected.", collectionDescriptionFontSize);
                }
                else
                {
                    collectionDescriptionLabel = new LabelOnlyInput(collectionDescription.ToString(), collectionDescriptionFontSize);
                }

                collectionDescriptionLabel.Padding = new Thickness(20, 0, 0, 0);
                inputs.Add(collectionDescriptionLabel);
            }
            else
            {
                ItemPickerPageInput probeSelectionPage = new ItemPickerPageInput("Load the following probes", enabledProbes.Select(s=>s.DisplayName).Cast<object>().ToList())
                {
                    DisplayNumber = false,
                    RandomizeItemOrder = false,
                    Multiselect = true,
                    SelectedItems = enabledProbes.Where(w=>w.Selected == true).Select(s => s.DisplayName).Cast<object>().ToList()
                };
                inputs.Add(probeSelectionPage);
            }

            // describe remote data storage
            if (_remoteDataStore != null)
            {
                inputs.Add(new LabelOnlyInput((_remoteDataStore as RemoteDataStore).StorageDescription, collectionDescriptionFontSize));
            }

            // don't repeatedly prompt the participant for their ID
            if (_startConfirmationMode == ProtocolStartConfirmationMode.None || !string.IsNullOrWhiteSpace(_participantId))
            {
                inputs.Add(new LabelOnlyInput("Tap Submit below to begin."));
            }
            else if (_startConfirmationMode == ProtocolStartConfirmationMode.RandomDigits)
            {
                inputs.Add(new SingleLineTextInput("To participate in this study as described above, please enter the following code:  " + new Random().Next(1000, 10000), "code", Keyboard.Numeric)
                {
                    DisplayNumber = false
                });
            }
            else if (_startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdDigits)
            {
                inputs.Add(new SingleLineTextInput("To participate in this study as described above, please enter your participant identifier below.", "code", Keyboard.Numeric)
                {
                    DisplayNumber = false
                });

                inputs.Add(new SingleLineTextInput("Please re-enter your participant identifier to confirm.", "confirm", Keyboard.Numeric)
                {
                    DisplayNumber = false
                });
            }
            else if (_startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdQrCode)
            {
                inputs.Add(new QrCodeInput(QrCodePrefix.SENSUS_PARTICIPANT_ID, "Participant ID:  ", false, "To participate in this study as described above, please scan your participant barcode."));
            }
            else if (_startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdText)
            {
                inputs.Add(new SingleLineTextInput("To participate in this study as described above, please enter your participant identifier below.", "code", Keyboard.Text)
                {
                    DisplayNumber = false
                });

                inputs.Add(new SingleLineTextInput("Please re-enter your participant identifier to confirm.", "confirm", Keyboard.Text)
                {
                    DisplayNumber = false
                });
            }

            List<Input> completedInputs = await SensusServiceHelper.Get().PromptForInputsAsync("Confirm Study Participation", inputs.ToArray(), null, true, null, "Are you sure you would like cancel your enrollment in this study?", null, null, false);

            bool start = false;

            if (completedInputs != null)
            {
                var selectedProbeObjs = completedInputs.Where(w => w.GetType() == typeof(ItemPickerPageInput)).FirstOrDefault()?.Value as List<object>;
                _probesSelectedOnStartUp = selectedProbeObjs?.Select(s => s.ToString())?.ToList() ?? new List<string>();
                if (AllowProbeDisableOnStartUp == true)
                {
                    if (selectedProbeObjs.Count == 0)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("You must select at least one probe to include in this study.  Cannot Start.");
                        start = false;
                    }
                }
                enabledProbes.ForEach(e => e.Selected = _probesSelectedOnStartUp == null || _probesSelectedOnStartUp.Contains(e.DisplayName));

                if (_startConfirmationMode == ProtocolStartConfirmationMode.None || !string.IsNullOrWhiteSpace(_participantId))
                {
                    start = true;
                }
                else if (_startConfirmationMode == ProtocolStartConfirmationMode.RandomDigits)
                {
                    Input codeInput = completedInputs.Last();
                    string codeInputValue = codeInput.Value as string;
                    int requiredCode = int.Parse(codeInput.LabelText.Substring(codeInput.LabelText.LastIndexOf(':') + 1));

                    if (int.TryParse(codeInputValue, out int codeInputValueInt) && codeInputValueInt == requiredCode)
                    {
                        start = true;
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Incorrect code entered.");
                    }
                }
                else if (_startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdDigits || _startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdText)
                {
                    string codeValue = completedInputs[completedInputs.Count - 2].Value as string;
                    string codeConfirmValue = completedInputs.Last().Value as string;
                    if (codeValue == codeConfirmValue)
                    {
                        if (string.IsNullOrWhiteSpace(codeValue))
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Your identifier cannot be blank.");
                        }
                        else
                        {
                            _participantId = codeValue;
                            start = true;
                        }
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("The identifiers that you entered did not match.");
                    }
                }
                else if (_startConfirmationMode == ProtocolStartConfirmationMode.ParticipantIdQrCode)
                {
                    string codeValue = completedInputs.Last().Value as string;
                    if (string.IsNullOrWhiteSpace(codeValue))
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Your participant barcode was empty.");
                    }
                    else
                    {
                        _participantId = codeValue;
                        start = true;
                    }
                }
            }

            if (start)
            {
                // create startup page, passing cancellation token that will be used for the startup.
                CancellationTokenSource startCancellationTokenSource = new CancellationTokenSource();
                _protocolStartPage = new ProtocolStartPage(startCancellationTokenSource);

                // wire up startup events

                _protocolStartInitiatedAsync = async () =>
                {
                    await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        INavigation navigation = (Application.Current as App).DetailPage.Navigation;
                        await navigation.PushModalAsync(_protocolStartPage);
                        _pushedProtocolStartPage = true;
                        await _protocolStartPage.SetProgressAsync(0);
                    });
                };

                _protocolStartAddProgressAsync = async (additionalProgress) =>
                {
                    await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        await _protocolStartPage.SetProgressAsync(_protocolStartPage.GetProgress() + additionalProgress);
                    });
                };

                _protocolStartFinishedAsync = async (state) =>
                {
                    if (_pushedProtocolStartPage)
                    {
                        await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            INavigation navigation = (Application.Current as App).DetailPage.Navigation;
                            if (navigation.ModalStack.First() == _protocolStartPage)
                            {
                                await navigation.PopModalAsync();
                            }

                            _pushedProtocolStartPage = false;
                        });
                    }

                    // if we started successfully on ios, warn the user not to terminate the app.
#if __IOS__
                    if (state == ProtocolState.Running)
                    {
                        await (Application.Current as App).DetailPage.DisplayAlert("Caution", "Please be careful not to terminate the app by swiping it away, as doing this will discontinue your participation in the study. Instead, move the app to the background by tapping the home button.", "OK");
                    }
#endif
                };

                try
                {
                    await StartAsync(startCancellationTokenSource.Token);
                }
                finally
                {
                    // ensure that startup callback events are unwired. don't want subsequent calls to StartAsync (e.g., on health test) to call them.
                    _protocolStartInitiatedAsync = null;
                    _protocolStartAddProgressAsync = null;
                    _protocolStartFinishedAsync = null;
                }
            }
        }

        public bool TimeIsWithinAlertExclusionWindow(TimeSpan time)
        {
            return _alertExclusionWindows.Any(window => window.Encompasses(time));
        }

        public async Task<List<AnalyticsTrackedEvent>> TestHealthAsync(bool userInitiated, CancellationToken cancellationToken)
        {
            List<AnalyticsTrackedEvent> events = new List<AnalyticsTrackedEvent>();
            string eventName;
            Dictionary<string, string> properties;

            eventName = TrackedEvent.Health + ":" + GetType().Name;
            properties = new Dictionary<string, string>
            {
                { "State", _state.ToString() }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            if (_state != ProtocolState.Starting && _state != ProtocolState.Running)
            {
                try
                {
                    await StopAsync();
                }
                catch (Exception)
                {
                }

                try
                {
                    await StartAsync(cancellationToken);
                }
                catch (Exception)
                {
                }
            }

            if (_state == ProtocolState.Running)
            {
                if (await _localDataStore.TestHealthAsync(events) == HealthTestResult.Restart)
                {
                    try
                    {
                        await _localDataStore.RestartAsync();
                    }
                    catch (Exception)
                    {
                    }
                }

                if (await _remoteDataStore.TestHealthAsync(events) == HealthTestResult.Restart)
                {
                    try
                    {
                        await _remoteDataStore.RestartAsync();
                    }
                    catch (Exception)
                    {
                    }
                }

                foreach (Probe probe in _probes)
                {
                    if (probe.Enabled && probe.Selected)
                    {
                        if (await probe.TestHealthAsync(events) == HealthTestResult.Restart)
                        {
                            try
                            {
                                await probe.RestartAsync();
                            }
                            catch (Exception)
                            {
                            }
                        }
                        else
                        {
                            // keep track of successful system-initiated health tests within the participation horizon. this 
                            // tells use how consistently the probe is running.
                            if (!userInitiated)
                            {
                                lock (probe.SuccessfulHealthTestTimes)
                                {
                                    probe.SuccessfulHealthTestTimes.Add(DateTime.Now);
                                    probe.SuccessfulHealthTestTimes.RemoveAll(healthTestTime => healthTestTime < ParticipationHorizon);
                                }
                            }
                        }
                    }
                }
            }

#if __ANDROID__
            eventName = TrackedEvent.Miscellaneous + ":" + GetType().Name;
            properties = new Dictionary<string, string>
            {
                { "Wake Lock Count", (SensusServiceHelper.Get() as Android.AndroidSensusServiceHelper).WakeLockAcquisitionCount.ToString() }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));
#endif

            ParticipationReportDatum participationReport = new ParticipationReportDatum(DateTimeOffset.UtcNow, this);
            SensusServiceHelper.Get().Logger.Log("Protocol report:" + Environment.NewLine + participationReport, LoggingLevel.Normal, GetType());

            _localDataStore.WriteDatum(participationReport, cancellationToken);

            return events;
        }

        public async Task StopAsync()
        {
            try
            {
                lock (this)
                {
                    if (_state == ProtocolState.Starting || _state == ProtocolState.Running)
                    {
                        _state = ProtocolState.Stopping;
                        FireStateChanged();
                    }
                    else
                    {
                        return;
                    }
                }

                SensusServiceHelper.Get().Logger.Log("Stopping protocol \"" + _name + "\".", LoggingLevel.Normal, GetType());

                // the user might have force-stopped the protocol before the scheduled stop fired. don't fire the scheduled stop.
                await CancelScheduledStopAsync();

                await SensusServiceHelper.Get().RemoveRunningProtocolIdAsync(_id);

                foreach (Probe probe in _probes)
                {
                    if (probe.Running)
                    {
                        try
                        {
                            await probe.StopAsync();
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to stop " + probe.GetType().FullName + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                }

                if (_localDataStore != null && _localDataStore.Running)
                {
                    try
                    {
                        await _localDataStore.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop local data store:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                if (_remoteDataStore != null && _remoteDataStore.Running)
                {
                    try
                    {
                        await _remoteDataStore.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop remote data store:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                await SensusServiceHelper.Get().UpdatePushNotificationRegistrationsAsync(default(CancellationToken));

                // save the state of the app, so that if it terminates unexpectedly (e.g., if the user swipes it away)
                // we won't attempt to restart the protocol if/when the app restarts.
                await SensusServiceHelper.Get().SaveAsync();

                SensusServiceHelper.Get().Logger.Log("Stopped protocol \"" + _name + "\".", LoggingLevel.Normal, GetType());
                await SensusServiceHelper.Get().FlashNotificationAsync("Stopped \"" + _name + "\".");

                _state = ProtocolState.Stopped;
                FireStateChanged();
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while stopping protocol:  " + ex.Message, ex);

                _state = ProtocolState.Stopped;
                FireStateChanged();
            }
        }

        public async Task DeleteAsync()
        {
            await SensusServiceHelper.Get().UnregisterProtocolAsync(this);

            try
            {
                Directory.Delete(StorageDirectory, true);
                _storageDirectory = null;
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to delete protocol storage directory:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Protocol && (obj as Protocol)._id == _id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return _name;
        }

        private void FireStateChanged()
        {
            StateChanged?.Invoke(this, _state);
            FireCaptionChanged();
        }

        private void FireCaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
        }

        private void FireSubCaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubCaption)));
        }
    }
}

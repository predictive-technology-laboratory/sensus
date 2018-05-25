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
using Sensus.UI;
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

        public static void Create(string name)
        {
            Protocol protocol = new Protocol(name);

            foreach (Probe probe in Probe.GetAll())
            {
                protocol.AddProbe(probe);
            }

            SensusServiceHelper.Get().RegisterProtocol(protocol);
        }

        public static void DeserializeAsync(Uri webURI, Action<Protocol> callback)
        {
            try
            {
                WebClient downloadClient = new WebClient();

#if __ANDROID__ || __IOS__
                downloadClient.DownloadDataCompleted += (o, e) =>
                {
                    if (e.Error == null)
                    {
                        DeserializeAsync(e.Result, callback);
                    }
                    else
                    {
                        string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + e.Error.Message;
                        SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                        SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);

                        callback?.Invoke(null);
                    }
                };
#elif LOCAL_TESTS
#else
#warning "Unrecognized platform"
#endif

                downloadClient.DownloadDataAsync(webURI);
            }
            catch (Exception ex)
            {
                string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);

                callback?.Invoke(null);
            }
        }

        public static void DeserializeAsync(byte[] bytes, Action<Protocol> callback)
        {
            try
            {
                string json = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(SensusContext.Current.SymmetricEncryption.DecryptToString(bytes));

                DeserializeAsync(json, async protocol =>
                {
                    try
                    {
                        // don't reset the protocol id -- received protocols should remain in the same study.
                        protocol.Reset(false);

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

                        // if we've previously registered the protocol, the user needs to decide what to do:  either keep the previous one or use the new one
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
                                registeredProtocol.Stop();
                                SensusServiceHelper.Get().UnregisterProtocol(registeredProtocol);
                                registeredProtocol = null;
                            }
                        }

                        // if we're not using a previously registered protocol, then we need to configure the new one.
                        if (registeredProtocol == null)
                        {
                            #region if grouped protocols are available, consider swapping the currely assigned one with another.
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
                        // if the protocol is null we'll have already flashed an error message with specific information. only need to flash a 
                        // message here if something went wrong after successfully deserializing the protocol.
                        if (protocol != null)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to set up deserialized protocol:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                            await SensusServiceHelper.Get().FlashNotificationAsync("Failed to set up unpacked protocol.");
                            protocol = null;
                        }
                    }
                    finally
                    {
                        callback?.Invoke(protocol);
                    }
                });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to decrypt/convert/deserialize protocol from bytes:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                SensusServiceHelper.Get().FlashNotificationAsync("Failed to unpack protocol.");
                callback?.Invoke(null);
            }
        }

        public static void DeserializeAsync(string json, Action<Protocol> callback)
        {
            Task.Run(() =>
            {
                Protocol protocol = null;

                try
                {
                    ManualResetEvent protocolWait = new ManualResetEvent(false);

                    // always deserialize protocols on the main thread (e.g., since a looper is required for android). also, disable
                    // flash notifications so we don't get any messages that result from properties being set within the protocol.
                    SensusServiceHelper.Get().FlashNotificationsEnabled = false;
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        try
                        {
                            protocol = JsonConvert.DeserializeObject<Protocol>(json, SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Error while deserializing protocol from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                        }
                        finally
                        {
                            protocolWait.Set();
                        }
                    });

                    protocolWait.WaitOne();

                    if (protocol == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to deserialize protocol from JSON.", LoggingLevel.Normal, typeof(Protocol));
                        SensusServiceHelper.Get().FlashNotificationAsync("Failed to unpack protocol from JSON.");
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to deserialize protocol from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                    SensusServiceHelper.Get().FlashNotificationAsync("Failed to unpack protocol from JSON:  " + ex.Message);
                }
                finally
                {
                    SensusServiceHelper.Get().FlashNotificationsEnabled = true;
                }

                callback?.Invoke(protocol);
            });
        }

        public static void DisplayAndStartAsync(Protocol protocol)
        {
            Task.Run(() =>
            {
                if (protocol == null)
                {
                    SensusServiceHelper.Get().FlashNotificationAsync("Protocol is empty. Cannot display or start it.");
                }
                else if (protocol.Running)
                {
                    SensusServiceHelper.Get().FlashNotificationAsync("The following study is currently running:  \"" + protocol.Name + "\".");
                }
                else
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        ProtocolsPage protocolsPage = null;

                        // display the protocols page if it isn't already up
                        INavigation navigation = Application.Current.MainPage.Navigation;
                        Page topPage = navigation.NavigationStack.Count > 0 ? navigation.NavigationStack.Last() : null;
                        if (topPage is ProtocolsPage)
                        {
                            protocolsPage = topPage as ProtocolsPage;
                        }
                        else
                        {
                            protocolsPage = new ProtocolsPage();
                        }

                        // ask user to start protocol
                        protocol.StartWithUserAgreementAsync("You just opened \"" + protocol.Name + "\" within Sensus.");
                    });
                }
            });
        }

        public static void RunUiTestingProtocol(Stream uiTestingProtocolFile)
        {
            try
            {
                // delete all current protocols -- we don't want them interfering with the one we're about to load/run.
                foreach (Protocol protocol in SensusServiceHelper.Get().RegisteredProtocols)
                {
                    protocol.Delete();
                }

                using (MemoryStream protocolStream = new MemoryStream())
                {
                    uiTestingProtocolFile.CopyTo(protocolStream);
                    string protocolJSON = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(SensusContext.Current.SymmetricEncryption.DecryptToString(protocolStream.ToArray()));
                    DeserializeAsync(protocolJSON, protocol =>
                    {
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

                        DisplayAndStartAsync(protocol);
                    });
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

        public event EventHandler<bool> ProtocolRunningChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private ScheduledCallback _scheduledStartCallback;
        private ScheduledCallback _scheduledStopCallback;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;
        private string _storageDirectory;
        private string _lockPasswordHash;
        private AnonymizedJsonContractResolver _jsonAnonymizer;
        private DateTimeOffset _randomTimeAnchor;
        private ConcurrentObservableCollection<PointOfInterest> _pointsOfInterest;
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
        private ProtocolStartConfirmationMode _protocolStartConfirmationMode;
        private string _participantId;

        private readonly object _locker = new object();

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// A descriptive name for the <see cref="Protocol"/>.
        /// </summary>
        /// <value>The name.</value>
        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                CaptionChanged();
            }
        }

        public List<Probe> Probes
        {
            get { return _probes; }
            set { _probes = value; }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        [JsonIgnore]
        public ScheduledCallback ScheduledStartCallback
        {
            get { return _scheduledStartCallback; }
        }

        [JsonIgnore]
        public ScheduledCallback ScheduledStopCallback
        {
            get { return _scheduledStopCallback; }
        }

        public LocalDataStore LocalDataStore
        {
            get { return _localDataStore; }
            set
            {
                if (value != _localDataStore)
                {
                    _localDataStore = value;
                    _localDataStore.Protocol = this;

                    _localDataStore.UpdatedCaptionText += (o, status) =>
                    {
                        SubCaptionChanged();
                    };
                }
            }
        }

        public RemoteDataStore RemoteDataStore
        {
            get { return _remoteDataStore; }
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
            get
            {
                return _lockPasswordHash;
            }
            set
            {
                _lockPasswordHash = value;
            }
        }

        public AnonymizedJsonContractResolver JsonAnonymizer
        {
            get { return _jsonAnonymizer; }
            set { _jsonAnonymizer = value; }
        }

        public DateTimeOffset RandomTimeAnchor
        {
            get
            {
                return _randomTimeAnchor;
            }
            set
            {
                _randomTimeAnchor = value;
            }
        }

        public ConcurrentObservableCollection<PointOfInterest> PointsOfInterest
        {
            get { return _pointsOfInterest; }
        }

        /// <summary>
        /// A detailed description of the <see cref="Protocol"/> (e.g., what it does, who it is intended for, etc.).
        /// </summary>
        /// <value>The description.</value>
        [EditorUiProperty(null, true, 15)]
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
            }
        }

        /// <summary>
        /// Whether or not to start the <see cref="Protocol"/> immediately after the user has opted into it.
        /// </summary>
        /// <value><c>true</c> to start immediately; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Start Immediately:", true, 16)]
        public bool StartImmediately
        {
            get
            {
                return _startImmediately;
            }
            set
            {
                _startImmediately = value;
            }
        }

        /// <summary>
        /// The date on which the <see cref="Protocol"/> will start running. Only has an effect if <see cref="StartImmediately"/> 
        /// is `false`.
        /// </summary>
        /// <value>The start date.</value>
        [DateUiProperty("Start Date:", true, 17)]
        public DateTime StartDate
        {
            get
            {
                return _startTimestamp;
            }
            set
            {
                _startTimestamp = new DateTime(value.Year, value.Month, value.Day, _startTimestamp.Hour, _startTimestamp.Minute, _startTimestamp.Second);

                CaptionChanged();
            }
        }

        /// <summary>
        /// The time at which the <see cref="Protocol"/> will start running. Only has an effect if <see cref="StartImmediately"/> is `false`.
        /// </summary>
        /// <value>The start time.</value>
        [TimeUiProperty("Start Time:", true, 18)]
        public TimeSpan StartTime
        {
            get
            {
                return _startTimestamp.TimeOfDay;
            }
            set
            {
                _startTimestamp = new DateTime(_startTimestamp.Year, _startTimestamp.Month, _startTimestamp.Day, value.Hours, value.Minutes, value.Seconds);

                CaptionChanged();
            }
        }

        /// <summary>
        /// Whether or not to execute the <see cref="Protocol"/> forever after it has started.
        /// </summary>
        /// <value><c>true</c> to execute forever; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Continue Indefinitely:", true, 19)]
        public bool ContinueIndefinitely
        {
            get
            {
                return _continueIndefinitely;
            }
            set
            {
                _continueIndefinitely = value;
            }
        }

        /// <summary>
        /// The date on which the <see cref="Protocol"/> will stop running. Only has an effect if <see cref="ContinueIndefinitely"/> is `false`.
        /// </summary>
        /// <value>The end date.</value>
        [DateUiProperty("End Date:", true, 20)]
        public DateTime EndDate
        {
            get
            {
                return _endTimestamp;
            }
            set
            {
                _endTimestamp = new DateTime(value.Year, value.Month, value.Day, _endTimestamp.Hour, _endTimestamp.Minute, _endTimestamp.Second);
            }
        }

        /// <summary>
        /// The time at which the <see cref="Protocol"/> will stop running. Only has an effect if <see cref="ContinueIndefinitely"/> is `false`.
        /// </summary>
        /// <value>The end time.</value>
        [TimeUiProperty("End Time:", true, 21)]
        public TimeSpan EndTime
        {
            get
            {
                return _endTimestamp.TimeOfDay;
            }
            set
            {
                _endTimestamp = new DateTime(_endTimestamp.Year, _endTimestamp.Month, _endTimestamp.Day, value.Hours, value.Minutes, value.Seconds);
            }
        }

        /// <summary>
        /// The number of days used to calculate the participation percentage. For example, if the participation horizon is
        /// 7 days, and the user has been running a <see cref="ListeningProbe"/> for 1 day, then the participation percentage
        /// would be 1/7 (~14%). On the other hand, if the participation horizon is 1 day, then the same user would have a 
        /// participation percentage of 1/1 (100%).
        /// </summary>
        /// <value>The participation horizon, in days.</value>
        [EntryIntegerUiProperty("Participation Horizon (Days):", true, 23)]
        public int ParticipationHorizonDays
        {
            get
            {
                return _participationHorizonDays;
            }
            set
            {
                _participationHorizonDays = value;
            }
        }

        [JsonIgnore]
        public DateTime ParticipationHorizon
        {
            get { return DateTime.Now.AddDays(-_participationHorizonDays); }
        }

        /// <summary>
        /// An email address for the individual who is responsible for handling questions
        /// associated with this study.
        /// </summary>
        /// <value>The contact email.</value>
        [EntryStringUiProperty("Contact Email:", true, 24)]
        public string ContactEmail
        {
            get
            {
                return _contactEmail;
            }
            set
            {
                _contactEmail = value;
            }
        }

        /// <summary>
        /// Whether the user should be allowed to group the <see cref="Protocol"/> with other <see cref="Protocol"/>s to form a 
        /// bundle that participant's are randomized into.
        /// </summary>
        /// <value><c>true</c> if groupable; otherwise, <c>false</c>.</value>
        [OnOffUiProperty(null, true, 25)]
        public bool Groupable
        {
            get
            {
                return _groupable;
            }
            set
            {
                _groupable = value;
            }
        }

        public List<Protocol> GroupedProtocols
        {
            get
            {
                return _groupedProtocols;
            }
            set
            {
                _groupedProtocols = value;
            }
        }

        /// <summary>
        /// The participation percentage required for a user to be considered eligible for rewards.
        /// </summary>
        /// <value>The reward threshold.</value>
        [EntryFloatUiProperty("Reward Threshold:", true, 26)]
        public float? RewardThreshold
        {
            get
            {
                return _rewardThreshold;
            }
            set
            {
                // if a threshold is given, force it to be in [0,1]
                if (value != null)
                {
                    float threshold = value.GetValueOrDefault();
                    if (threshold < 0)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync("Reward threshold must be between 0 and 1.");
                        value = 0;
                    }
                    else if (threshold > 1)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync("Reward threshold must be between 0 and 1.");
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
        [EntryFloatUiProperty("GPS - Desired Accuracy (Meters):", true, 27)]
        public float GpsDesiredAccuracyMeters
        {
            get { return _gpsDesiredAccuracyMeters; }
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
        [EntryIntegerUiProperty("GPS - Minimum Time Delay (MS):", true, 28)]
        public int GpsMinTimeDelayMS
        {
            get { return _gpsMinTimeDelayMS; }
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
        [EntryFloatUiProperty("GPS - Minimum Distance Delay (Meters):", true, 29)]
        public float GpsMinDistanceDelayMeters
        {
            get
            {
                return _gpsMinDistanceDelayMeters;
            }
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
            get
            {
                return _variableValue;
            }
            set
            {
                _variableValue = value;
            }
        }

        /// <summary>
        /// A <see cref="Protocol"/> may delare variables whose values can be easily reused throughout the
        /// system. For example, if many of the survey inputs share a particular substring (e.g., the study 
        /// name), consider defining a variable named `study-name` that holds the study name. You can then
        /// reference this variable when defining the survey input label via `{study-name}`. The format
        /// of this field is `variable-name:variable-value`.
        /// </summary>
        /// <value>The variable value user interface property.</value>
        [EditableListUiProperty("Variables:", true, 30)]
        [JsonIgnore]
        public List<string> VariableValueUiProperty
        {
            get
            {
                return _variableValue.Select(kvp => kvp.Key + ": " + kvp.Value).ToList();
            }
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
        /// The user can be asked to confirm starting the <see cref="Protocol"/> in serveral ways. See <see cref="ProtocolStartConfirmationMode"/>
        /// for more information.
        /// </summary>
        /// <value>The protocol start confirmation mode.</value>
        [ListUiProperty("Start Confirmation Mode:", true, 31, new object[] { ProtocolStartConfirmationMode.None, ProtocolStartConfirmationMode.RandomDigits, ProtocolStartConfirmationMode.UserIdDigits, ProtocolStartConfirmationMode.UserIdText, ProtocolStartConfirmationMode.UserIdQrCode })]
        public ProtocolStartConfirmationMode ProtocolStartConfirmationMode
        {
            get
            {
                return _protocolStartConfirmationMode;
            }
            set
            {
                _protocolStartConfirmationMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the participant identifier.
        /// </summary>
        /// <value>The participant identifier.</value>
        public string ParticipantId
        {
            get
            {
                return _participantId;
            }
            set
            {
                _participantId = value;
            }
        }

        #region iOS-specific protocol properties

#if __IOS__
        [OnOffUiProperty("GPS - Pause Location Updates:", true, 30)]
        public bool GpsPauseLocationUpdatesAutomatically { get; set; } = false;

        [ListUiProperty("GPS - Pause Activity Type:", true, 31, new object[] { ActivityType.Other, ActivityType.AutomotiveNavigation, ActivityType.Fitness, ActivityType.OtherNavigation })]
        public ActivityType GpsPauseActivityType { get; set; } = ActivityType.Other;

        [OnOffUiProperty("GPS - Significant Changes:", true, 32)]
        public bool GpsListenForSignificantChanges { get; set; } = false;

        [OnOffUiProperty("GPS - Defer Location Updates:", true, 33)]
        public bool GpsDeferLocationUpdates { get; set; } = false;

        private float _gpsDeferralDistanceMeters = 500;

        [EntryFloatUiProperty("GPS - Deferral Distance (Meters):", true, 34)]
        public float GpsDeferralDistanceMeters
        {
            get
            {
                return _gpsDeferralDistanceMeters;
            }
            set
            {
                if (value < 0)
                    value = -1;

                _gpsDeferralDistanceMeters = value;
            }
        }

        private float _gpsDeferralTimeMinutes = 5;

        [EntryFloatUiProperty("GPS - Deferral Time (Mins.):", true, 35)]
        public float GpsDeferralTimeMinutes
        {
            get { return _gpsDeferralTimeMinutes; }
            set
            {
                if (value < 0)
                    value = -1;

                _gpsDeferralTimeMinutes = value;
            }
        }
#endif

        /// <summary>
        /// A comma-separated list of time windows during which alerts from Sensus (e.g., notifications
        /// about new surveys) should not have a sound or vibration associated with them. The format
        /// is the same as described for <see cref="ScriptRunner.TriggerWindowsString"/>, except that 
        /// exact times (e.g., 11:32am) do not make any sense -- only windows (e.g., 11:32am-1:00pm) do.
        /// </summary>
        /// <value>The alert exclusion window string.</value>
        [EntryStringUiProperty("Alert Exclusion Windows:", true, 36)]
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
        [EntryStringUiProperty("Asymmetric Encryption Public Key:", true, 37)]
        public string AsymmetricEncryptionPublicKey
        {
            get
            {
                return _asymmetricEncryptionPublicKey;
            }
            set
            {
                _asymmetricEncryptionPublicKey = value?.Trim().Replace("\n", "").Replace(" ", "");
            }
        }

        [JsonIgnore]
        public AsymmetricEncryption AsymmetricEncryption
        {
            get
            {
                return new AsymmetricEncryption(_asymmetricEncryptionPublicKey);
            }
        }

        #endregion

        /// <summary>
        /// Whether or not to allow the user to view data being collected by the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow View Data:", true, 38)]
        public bool AllowViewData { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to manually submit data being collected by the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Submit Data:", true, 39)]
        public bool AllowSubmitData { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to display/scan participation QR codes for the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Participation Scanning:", true, 40)]
        public bool AllowParticipationScanning { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to copy the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Copy:", true, 41)]
        public bool AllowCopy { get; set; } = false;

        /// <summary>
        /// Whether or not to allow the user to share the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> to allow; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Share:", true, 42)]
        public bool Shareable { get; set; } = false;

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

        [JsonIgnore]
        public string Caption
        {
            get
            {
                return Name + " (" + (Running ? "Running" : (ScheduledStartCallback == null ? "Stopped" : "Scheduled: " + StartDate.ToShortDateString() + " " + (StartDate.Date + StartTime).ToShortTimeString())) + ")";
            }
        }

        [JsonIgnore]
        public string SubCaption
        {
            get
            {
                return _localDataStore?.CaptionText;
            }
        }

        /// <summary>
        /// For JSON deserialization
        /// </summary>
        private Protocol()
        {
            _running = false;
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
            _protocolStartConfirmationMode = ProtocolStartConfirmationMode.None;
        }

        /// <summary>
        /// Called by static CreateAsync. Should not be called directly by outside callers.
        /// </summary>
        /// <param name="name">Name.</param>
        public Protocol(string name) : this()
        {
            _name = name;
            _probes = new List<Probe>();

            Reset(true);
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

        private void Reset(bool resetId)
        {
            // reset id and storage directory (directory might exist if deserializing the same protocol multiple times)
            if (resetId)
            {
                _id = Guid.NewGuid().ToString();
            }

            ResetStorageDirectory();

            // pick a random time anchor within the first 1000 years AD. we got a strange exception in insights about the resulting datetime having a year
            // outside of [0,10000]. no clue how this could happen, but we'll guard against it all the same.
            try
            {
                _randomTimeAnchor = new DateTimeOffset((long)(new Random().NextDouble() * new DateTimeOffset(1000, 1, 1, 0, 0, 0, new TimeSpan()).Ticks), new TimeSpan());
            }
            catch (Exception) { }

            // reset probes
            foreach (Probe probe in _probes)
            {
                probe.Reset();

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

        public void CopyAsync(bool resetId, bool register, Action<Protocol> callback = null)
        {
            DeserializeAsync(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), protocol =>
            {
                protocol.Reset(resetId);

                if (register)
                {
                    SensusServiceHelper.Get().RegisterProtocol(protocol);
                }

                callback?.Invoke(protocol);
            });
        }

        public void StartAsync(Action callback = null)
        {
            Task.Run(() =>
            {
                Start();
                callback?.Invoke();
            });
        }

        private void StartInternal()
        {
            lock (_locker)
            {
                if (_running)
                {
                    return;
                }
                else
                {
                    _running = true;
                }

                _scheduledStartCallback = null;

                CaptionChanged();

                ProtocolRunningChanged?.Invoke(this, _running);

                SensusServiceHelper.Get().AddRunningProtocolId(_id);

                bool stopProtocol = false;

                // start local data store
                try
                {
                    if (_localDataStore == null)
                    {
                        throw new Exception("Local data store not defined.");
                    }

                    _localDataStore.Start();

                    // start remote data store
                    try
                    {
                        if (_remoteDataStore == null)
                        {
                            throw new Exception("Remote data store not defined.");
                        }

                        _remoteDataStore.Start();

                        // start probes
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
                                    ManualResetEvent authorizationWait = new ManualResetEvent(false);
                                    new HKHealthStore().RequestAuthorizationToShare(new NSSet(), objectTypesToRead,
                                        (success, error) =>
                                        {
                                            if (error != null)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Error while requesting HealthKit authorization:  " + error.Description, LoggingLevel.Normal, GetType());
                                            }

                                            authorizationWait.Set();
                                        });

                                    authorizationWait.WaitOne();
                                }
                            }
#endif

                            SensusServiceHelper.Get().Logger.Log("Starting probes for protocol " + _name + ".", LoggingLevel.Normal, GetType());
                            int probesEnabled = 0;
                            bool startMicrosoftBandProbes = true;
                            foreach (Probe probe in _probes)
                            {
                                if (probe.Enabled)
                                {
                                    if (probe is MicrosoftBandProbeBase && !startMicrosoftBandProbes)
                                    {
                                        continue;
                                    }

                                    try
                                    {
                                        probe.Start();
                                    }
                                    catch (MicrosoftBandClientConnectException)
                                    {
                                        // if we failed to start a microsoft band probe due to a client connect exception, don't attempt to start the other
                                        // band probes. instead, rely on the band health check to periodically attempt to connect to the band. if and when this
                                        // succeeds, all band probes will then be started.
                                        startMicrosoftBandProbes = false;
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    // probe might become disabled during Start due to a NotSupportedException
                                    if (probe.Enabled)
                                    {
                                        ++probesEnabled;
                                    }
                                }
                            }

                            if (probesEnabled == 0)
                            {
                                throw new Exception("No probes were enabled.");
                            }
                            else
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("Started \"" + _name + "\".");
                            }
                        }
                        catch (Exception ex)
                        {
                            string message = "Failure while starting probes:  " + ex.Message;
                            SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                            SensusServiceHelper.Get().FlashNotificationAsync(message);
                            stopProtocol = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = "Remote data store failed to start:  " + ex.Message;
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        SensusServiceHelper.Get().FlashNotificationAsync(message);
                        stopProtocol = true;
                    }
                }
                catch (Exception ex)
                {
                    string message = "Local data store failed to start:  " + ex.Message;
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                    SensusServiceHelper.Get().FlashNotificationAsync(message);
                    stopProtocol = true;
                }

                if (stopProtocol)
                {
                    Stop();
                }
            }
        }

        public void Start()
        {
            if (_startImmediately || (DateTime.Now > _startTimestamp))
            {
                StartInternal();
            }
            else
            {
                ScheduleStart();
            }

            if (!_continueIndefinitely)
            {
                ScheduleStop();
            }
        }

        public void ScheduleStart()
        {
            TimeSpan timeUntilStart = _startTimestamp - DateTime.Now;

            _scheduledStartCallback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                return Task.Run(() =>
                {
                    StartInternal();
                    _scheduledStartCallback = null;
                });

            }, timeUntilStart, "START", _id, this, null,
#if __ANDROID__
            $"Started study: {Name}.");
#elif __IOS__
            $"Please open to start study {Name}.");
#else
            $"Started study: {Name}.");
#endif

            SensusContext.Current.CallbackScheduler.ScheduleCallback(_scheduledStartCallback);

            CaptionChanged();
        }

        public void CancelScheduledStart()
        {
            SensusContext.Current.CallbackScheduler.UnscheduleCallback(_scheduledStartCallback);
            _scheduledStartCallback = null;
            CaptionChanged();

            // we might have scheduled a stop when starting the protocol, so be sure to cancel it.
            CancelScheduledStop();
        }

        public void ScheduleStop()
        {
            TimeSpan timeUntilStop = _endTimestamp - DateTime.Now;

            _scheduledStopCallback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                return Task.Run(() =>
                {
                    Stop();
                    _scheduledStopCallback = null;
                });

            }, timeUntilStop, "STOP", _id, this, null,
#if __ANDROID__
            $"Stopped study: {Name}.");
#elif __IOS__
            $"Please open to stop study: {Name}.");
#else
            $"Stopped study: {Name}.");
#endif

            SensusContext.Current.CallbackScheduler.ScheduleCallback(_scheduledStopCallback);
        }

        public void CancelScheduledStop()
        {
            SensusContext.Current.CallbackScheduler.UnscheduleCallback(_scheduledStopCallback);
            _scheduledStopCallback = null;
        }

        public void StartWithUserAgreementAsync(string startupMessage, Action callback = null)
        {
            if (!_probes.Any(probe => probe.Enabled))
            {
                SensusServiceHelper.Get().FlashNotificationAsync("Probes not enabled. Cannot start.");
                callback?.Invoke();
                return;
            }

            if (!_continueIndefinitely && _endTimestamp <= DateTime.Now)
            {
                SensusServiceHelper.Get().FlashNotificationAsync("You cannot start this study because it has already ended.");
                callback?.Invoke();
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

            StringBuilder collectionDescription = new StringBuilder();
            foreach (Probe probe in _probes.OrderBy(probe => probe.DisplayName))
            {
                if (probe.Enabled && probe.StoreData)
                {
                    string probeCollectionDescription = probe.CollectionDescription;
                    if (!string.IsNullOrWhiteSpace(probeCollectionDescription))
                    {
                        collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + probeCollectionDescription);
                    }
                }
            }

            LabelOnlyInput collectionDescriptionLabel = null;
            int collectionDescriptionFontSize = 15;
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

            if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.None)
            {
                inputs.Add(new LabelOnlyInput("Tap Submit below to begin."));
            }
            else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.RandomDigits)
            {
                inputs.Add(new SingleLineTextInput("To participate in this study as described above, please enter the following code:  " + new Random().Next(1000, 10000), "code", Keyboard.Numeric)
                {
                    DisplayNumber = false
                });
            }
            else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdDigits)
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
            else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdQrCode)
            {
                inputs.Add(new QrCodeInput(QrCodePrefix.SENSUS_PARTICIPANT_ID, "Participant ID:  ", "To participate in this study as described above, please scan your participant barcode."));
            }
            else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdText)
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

            SensusServiceHelper.Get().PromptForInputsAsync(
                "Confirm Study Participation",
                inputs.ToArray(),
                null,
                true,
                null,
                "Are you sure you would like cancel your enrollment in this study?",
                null,
                null,
                false,
                completedInputs =>
                {
                    bool start = false;

                    if (completedInputs != null)
                    {
                        if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.None)
                        {
                            start = true;
                        }
                        else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.RandomDigits)
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
                                SensusServiceHelper.Get().FlashNotificationAsync("Incorrect code entered.");
                            }
                        }
                        else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdDigits ||
                                 _protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdText)
                        {
                            string codeValue = completedInputs[completedInputs.Count - 2].Value as string;
                            string codeConfirmValue = completedInputs.Last().Value as string;
                            if (codeValue == codeConfirmValue)
                            {
                                if (string.IsNullOrWhiteSpace(codeValue))
                                {
                                    SensusServiceHelper.Get().FlashNotificationAsync("Your identifier cannot be blank.");
                                }
                                else
                                {
                                    _participantId = codeValue;
                                    start = true;
                                }
                            }
                            else
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("The identifiers that you entered did not match.");
                            }
                        }
                        else if (_protocolStartConfirmationMode == ProtocolStartConfirmationMode.UserIdQrCode)
                        {
                            string codeValue = completedInputs.Last().Value as string;
                            if (string.IsNullOrWhiteSpace(codeValue))
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("Your participant barcode was empty.");
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
                        Start();
                    }

                    callback?.Invoke();
                });
        }

        public bool TimeIsWithinAlertExclusionWindow(TimeSpan time)
        {
            return _alertExclusionWindows.Any(window => window.Encompasses(time));
        }

        public Task<List<Tuple<string, Dictionary<string, string>>>> TestHealthAsync(bool userInitiated, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() =>
            {
                ParticipationReportDatum participationReport;

                List<Tuple<string, Dictionary<string, string>>> events = new List<Tuple<string, Dictionary<string, string>>>();
                string eventName;
                Dictionary<string, string> properties;

                lock (_locker)
                {
                    eventName = TrackedEvent.Health + ":" + GetType().Name;
                    properties = new Dictionary<string, string>
                    {
                        { "Running", _running.ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);

                    events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));

                    if (!_running)
                    {
                        try
                        {
                            Stop();
                            Start();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (_running)
                    {
                        if (_localDataStore.TestHealth(ref events))
                        {
                            try
                            {
                                _localDataStore.Restart();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (_remoteDataStore.TestHealth(ref events))
                        {
                            try
                            {
                                _remoteDataStore.Restart();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        foreach (Probe probe in _probes)
                        {
                            if (probe.Enabled)
                            {
                                if (probe.TestHealth(ref events))
                                {
                                    try
                                    {
                                        probe.Restart();
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
                        { "Wake Lock Count", (SensusServiceHelper.Get() as Android.IAndroidSensusServiceHelper).WakeLockAcquisitionCount.ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);

                    events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));
#endif

                    participationReport = new ParticipationReportDatum(DateTimeOffset.UtcNow, this);
                    SensusServiceHelper.Get().Logger.Log("Protocol report:" + Environment.NewLine + participationReport, LoggingLevel.Normal, GetType());
                }

                _localDataStore.WriteDatum(participationReport, cancellationToken);

                return events;
            });
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                Stop();
            });
        }

        public void Stop()
        {
            lock (_locker)
            {
                if (_running)
                {
                    _running = false;
                    CaptionChanged();
                }
                else
                {
                    return;
                }

                SensusServiceHelper.Get().Logger.Log("Stopping protocol \"" + _name + "\".", LoggingLevel.Normal, GetType());

                ProtocolRunningChanged?.Invoke(this, _running);

                // the user might have force-stopped the protocol before the scheduled stop fired. don't fire the scheduled stop.
                CancelScheduledStop();

                SensusServiceHelper.Get().RemoveRunningProtocolId(_id);

                foreach (Probe probe in _probes)
                {
                    if (probe.Running)
                    {
                        try
                        {
                            probe.Stop();
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
                        _localDataStore.Stop();
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
                        _remoteDataStore.Stop();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop remote data store:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                SensusServiceHelper.Get().Logger.Log("Stopped protocol \"" + _name + "\".", LoggingLevel.Normal, GetType());
                SensusServiceHelper.Get().FlashNotificationAsync("Stopped \"" + _name + "\".");
            }
        }

        public Task DeleteAsync()
        {
            return Task.Run(() =>
            {
                Delete();
            });
        }

        public void Delete()
        {
            SensusServiceHelper.Get().UnregisterProtocol(this);

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

        private void CaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
        }

        private void SubCaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubCaption)));
        }
    }
}

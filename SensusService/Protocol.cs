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
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusService.Probes;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Xamarin.Forms;
using SensusService.Anonymization;
using System.Linq;
using System.Reflection;
using SensusUI;
using SensusService.Probes.Location;
using SensusService.Exceptions;
using SensusUI.Inputs;
using SensusService.Probes.User;
using SensusService.Probes.Apps;
using SensusService.Probes.Movement;
using System.Text;
using Plugin.Geolocator.Abstractions;

#if __IOS__
using HealthKit;
using Sensus.iOS.Probes.User.Health;
using Foundation;
#endif

namespace SensusService
{
    /// <summary>
    /// Container for probes, data stores, and all other information needed to run a collection experiment.
    /// </summary>
    public class Protocol
    {
        #region static members

        public const int GPS_DEFAULT_ACCURACY_METERS = 25;
        public const int GPS_DEFAULT_MIN_TIME_DELAY_MS = 5000;
        public const int GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS = 50;

        public static void CreateAsync(string name, Action<Protocol> callback)
        {
            Probe.GetAllAsync(probes =>
                {
                    Protocol protocol = new Protocol(name);

                    foreach (Probe probe in probes)
                        protocol.AddProbe(probe);

                    SensusServiceHelper.Get().RegisterProtocol(protocol);

                    if (callback != null)
                        callback(protocol);
                });
        }

        public static void DeserializeAsync(Uri webURI, bool useRandomGroupedProtocolIfAvailable, Action<Protocol> callback)
        {
            try
            {
                WebClient downloadClient = new WebClient();

                #if __ANDROID__ || __IOS__
                downloadClient.DownloadDataCompleted += (o, e) =>
                {
                    if (e.Error == null)
                        DeserializeAsync(e.Result, useRandomGroupedProtocolIfAvailable, callback);
                    else
                    {
                        string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + e.Error.Message;
                        SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                        SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);

                        if (callback != null)
                            callback(null);
                    }
                };
                #elif WINDOWS_PHONE
                // TODO:  Read bytes and display.
                #else
                #error "Unrecognized platform."
                #endif

                downloadClient.DownloadDataAsync(webURI);
            }
            catch (Exception ex)
            {
                string errorMessage = "Failed to download protocol from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);

                if (callback != null)
                    callback(null);
            }
        }

        public static void DeserializeAsync(byte[] bytes, bool useRandomGroupedProtocolIfAvailable, Action<Protocol> callback)
        {
            try
            {
                DeserializeAsync(SensusServiceHelper.Decrypt(bytes), useRandomGroupedProtocolIfAvailable, callback);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to decrypt protocol from bytes:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                SensusServiceHelper.Get().FlashNotificationAsync("Failed to decrypt protocol.");

                if (callback != null)
                    callback(null);
            }                        
        }

        public static void DeserializeAsync(string json, bool useRandomGroupedProtocolIfAvailable, Action<Protocol> callback)
        {
            new Thread(() =>
                {
                    Protocol protocol = null;

                    try
                    {
                        json = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(json);                        

                        ManualResetEvent protocolWait = new ManualResetEvent(false);

                        // always deserialize protocols on the main thread (e.g., since a looper is required for android)
                        Device.BeginInvokeOnMainThread(() =>
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
                            SensusServiceHelper.Get().FlashNotificationAsync("Failed to deserialize protocol from JSON.");
                        }
                        else
                        {     
                            // see if we have already registered the newly deserialized protocol. when considering whether a registered
                            // protocol is the match for the newly deserialized one, also check the protocols grouped with the registered
                            // protocol. from the user's perspective these grouped protocols are not visible, but they should trigger
                            // a match from an experimental perspective.
                            Protocol registeredProtocol = null;
                            foreach (Protocol p in SensusServiceHelper.Get().RegisteredProtocols)
                                if (p.Equals(protocol) || p.GroupedProtocols.Any(groupedProtocol => groupedProtocol.Equals(protocol)))
                                {
                                    registeredProtocol = p;
                                    break;
                                }

                            // if we haven't registered the protocol, then set it up and register it
                            if (registeredProtocol == null)
                            {
                                // if randomizing and grouped protocols are available, replace the protocol with one randomly selected from those available
                                if (useRandomGroupedProtocolIfAvailable && protocol.GroupedProtocols.Count > 0)
                                {
                                    Random r = new Random();
                                    int numProtocols = 1 + protocol.GroupedProtocols.Count;
                                    int protocolIndex = r.Next(0, numProtocols);

                                    // if protocol index == 0, then we should use the deserialized protocol -- no action is needed. if, on the other hand
                                    // the protocol index > 0, then we need to swap in a new protocol.
                                    if (protocolIndex > 0)
                                    {
                                        int replacementIndex = protocolIndex - 1;
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

                                // reset the random time anchor -- we shouldn't use the same one that someone else used
                                protocol.ResetRandomTimeAnchor();

                                // reset the storage directory
                                protocol.StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), protocol.Id);
                                if (!Directory.Exists(protocol.StorageDirectory))
                                    Directory.CreateDirectory(protocol.StorageDirectory);                                                       

                                // add any probes for the current platform that didn't come through when deserializing. for example, android has a listening WLAN probe, but iOS has a polling WLAN probe. neither will come through on the other platform when deserializing, since the types are not defined.
                                ManualResetEvent probeSetupWait = new ManualResetEvent(false);

                                Probe.GetAllAsync(probes =>
                                    {                                        
                                        List<Type> deserializedProbeTypes = protocol.Probes.Select(p => p.GetType()).ToList();

                                        foreach (Probe probe in probes)
                                            if (!deserializedProbeTypes.Contains(probe.GetType()))
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Adding missing probe to protocol:  " + probe.GetType().FullName, LoggingLevel.Normal, typeof(Protocol));
                                                protocol.AddProbe(probe);
                                            }     

                                        probeSetupWait.Set();
                                    });

                                probeSetupWait.WaitOne();

                                SensusServiceHelper.Get().RegisterProtocol(protocol);
                            }
                            else
                                protocol = registeredProtocol;

                            // protocols deserialized upon receipt (i.e., those here) are never groupable for experimental integrity reasons. we
                            // do not want the user to be able to group the newly deserialized protocol with other protocols and then share the 
                            // resulting grouped protocol with other participants. the user's only option is to share the protocol as-is. of course,
                            // if the protocol is unlocked then the user will be able to go edit the protocol and make it groupable. this is why
                            // all protocols should be locked before deployment in an experiment.
                            protocol.Groupable = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to deserialize protocol from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(Protocol));
                        SensusServiceHelper.Get().FlashNotificationAsync("Failed to deserialize protocol from JSON:  " + ex.Message);
                    }

                    if (callback != null)
                        callback(protocol);

                }).Start();
        }

        public static void DisplayAndStartAsync(Protocol protocol)
        {
            new Thread(() =>
                {
                    if (protocol == null)
                        SensusServiceHelper.Get().FlashNotificationAsync("Protocol is empty. Cannot display or start it.");
                    else if (protocol.Running)
                        SensusServiceHelper.Get().FlashNotificationAsync("You are already participating in \"" + protocol.Name + "\".");
                    else
                    {                        
                        Device.BeginInvokeOnMainThread(async () =>
                            {
                                // display the protocols page if it isn't already up
                                ProtocolsPage protocolsPage = null;
                                Page topPage = App.Current.MainPage.Navigation.NavigationStack.Last();
                                if (topPage is ProtocolsPage)
                                    protocolsPage = topPage as ProtocolsPage;
                                else
                                {
                                    protocolsPage = new ProtocolsPage();
                                    await App.Current.MainPage.Navigation.PushAsync(protocolsPage);
                                }

                                // ask user to start protocol
                                protocol.StartWithUserAgreementAsync("You just opened \"" + protocol.Name + "\" within Sensus.", () =>
                                    {
                                        Device.BeginInvokeOnMainThread(() =>
                                            {
                                                // rebind to pick up any color changes
                                                protocolsPage.Bind();
                                            });
                                    });
                            });
                    }

                }).Start();
        }

        public static void RunUnitTestingProtocol(Stream unitTestingProtocolFile)
        {
            try
            {
                // delete all current protocols -- we don't want them interfering with the one we're about to load/run.
                foreach (Protocol protocol in SensusServiceHelper.Get().RegisteredProtocols)
                    protocol.Delete();

                using (MemoryStream protocolStream = new MemoryStream())
                {
                    unitTestingProtocolFile.CopyTo(protocolStream);
                    string protocolJSON = SensusServiceHelper.Decrypt(protocolStream.ToArray());
                    DeserializeAsync(protocolJSON, false, protocol =>
                        {
                            if (protocol == null)
                                throw new Exception("Failed to deserialize unit testing protocol.");

                            foreach (Probe probe in protocol.Probes)
                            {
                                // unit testing is problematic with probes that take us away from Sensus, since it's difficult to automate UI 
                                // interaction outside of Sensus. disable any probes that might take us away from Sensus.

                                if (probe is FacebookProbe)
                                    probe.Enabled = false;

                                #if __IOS__
                                if (probe is iOSHealthKitProbe)
                                    probe.Enabled = false;
                                #endif

                                // clear the run-times collection from any script runners. need a clean start, just in case we have one-shot scripts
                                // that need to run every unit testing execution.
                                if (probe is ScriptProbe)
                                    foreach (ScriptRunner scriptRunner in (probe as ScriptProbe).ScriptRunners)
                                        scriptRunner.RunTimes.Clear();

                                // disable the accelerometer probe, since we use it to trigger a test script that can interrupt UI scripting.
                                if (probe is AccelerometerProbe)
                                    probe.Enabled = false;
                            }

                            DisplayAndStartAsync(protocol);
                        });
                }
            }
            catch (Exception ex)
            {
                string message = "Failed to run unit testing protocol:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, typeof(Protocol));
                throw new Exception(message);
            }
        }

        #endregion

        public event EventHandler<bool> ProtocolRunningChanged;

        private string _id;
        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;
        private string _storageDirectory;
        private ProtocolReportDatum _mostRecentReport;
        private bool _forceProtocolReportsToRemoteDataStore;
        private string _lockPasswordHash;
        private AnonymizedJsonContractResolver _jsonAnonymizer;
        private DateTimeOffset _randomTimeAnchor;
        private bool _shareable;
        private List<PointOfInterest> _pointsOfInterest;
        private string _description;
        private int _participationHorizonDays;
        private string _contactEmail;
        private bool _groupable;
        private List<Protocol> _groupedProtocols;
        private float? _rewardThreshold;
        private float _gpsDesiredAccuracyMeters;
        private int _gpsMinTimeDelayMS;
        private float _gpsMinDistanceDelayMeters;

        private readonly object _locker = new object();

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
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

        public LocalDataStore LocalDataStore
        {
            get { return _localDataStore; }
            set
            {
                if (value != _localDataStore)
                {
                    _localDataStore = value;
                    _localDataStore.Protocol = this;
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
            get { return _storageDirectory; }
            set { _storageDirectory = value; }
        }

        [JsonIgnore]
        public ProtocolReportDatum MostRecentReport
        {
            get { return _mostRecentReport; }
            set { _mostRecentReport = value; }
        }

        [OnOffUiProperty("Force Reports to Remote:", true, 20)]
        public bool ForceProtocolReportsToRemoteDataStore
        {
            get { return _forceProtocolReportsToRemoteDataStore; }
            set { _forceProtocolReportsToRemoteDataStore = value; }
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

        [OnOffUiProperty("Shareable:", true, 10)]
        public bool Shareable
        {
            get
            {
                return _shareable;
            }
            set
            {
                _shareable = value;
            }
        }

        public List<PointOfInterest> PointsOfInterest
        {
            get { return _pointsOfInterest; }
        }

        [EditorUiProperty("Description:", true, 15)]
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

        [EntryIntegerUiProperty("Participation Horizon (Days):", true, 16)]
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

        [EntryStringUiProperty("Contact Email:", true, 18)]
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

        [OnOffUiProperty(null, true, 19)]
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

        [EntryFloatUiProperty("Reward Threshold:", true, 20)]
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
        public float Participation
        {
            get
            { 
                float[] participations = _probes.Select(probe => probe.GetParticipation())
                                                .Where(participation => participation != null)
                                                .Select(participation => participation.GetValueOrDefault())
                                                .ToArray();

                // there will not be any participations if all probes are disabled -- perfect participation by definition
                if (participations.Length == 0)
                    return 1;
                else
                    return participations.Average();
            }
        }

        [EntryFloatUiProperty("GPS - Desired Accuracy (Meters):", true, 21)]
        public float GpsDesiredAccuracyMeters
        {
            get { return _gpsDesiredAccuracyMeters; }
            set
            {
                if (value <= 0)
                    value = GPS_DEFAULT_ACCURACY_METERS;

                _gpsDesiredAccuracyMeters = value;
            }
        }

        [EntryIntegerUiProperty("GPS - Minimum Time Delay (MS):", true, 22)]
        public int GpsMinTimeDelayMS
        {
            get { return _gpsMinTimeDelayMS; }
            set
            {
                if (value < 0)
                    value = GPS_DEFAULT_MIN_TIME_DELAY_MS;

                _gpsMinTimeDelayMS = value;
            }
        }

        [EntryFloatUiProperty("GPS - Minimum Distance Delay (Meters):", true, 23)]
        public float GpsMinDistanceDelayMeters
        {
            get
            {
                return _gpsMinDistanceDelayMeters;
            }
            set
            {
                if (value < 0)
                    value = GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS;

                _gpsMinDistanceDelayMeters = value;
            }
        }

        #region iOS-specific protocol properties

        #if __IOS__
        [OnOffUiProperty("GPS - Pause Location Updates:", true, 25)]
        public bool GpsPauseLocationUpdatesAutomatically { get; set; } = false;

        [ListUiProperty("GPS - Pause Activity Type:", true, 26, new object[] { ActivityType.Other, ActivityType.AutomotiveNavigation, ActivityType.Fitness, ActivityType.OtherNavigation })]
        public ActivityType GpsPauseActivityType { get; set; } = ActivityType.Other;

        [OnOffUiProperty("GPS - Significant Changes:", true, 27)]
        public bool GpsListenForSignificantChanges { get; set; } = false;

        [OnOffUiProperty("GPS - Defer Location Updates:", true, 28)]
        public bool GpsDeferLocationUpdates { get; set; } = false;

        private float _gpsDeferralDistanceMeters = 500;

        [EntryFloatUiProperty("GPS - Deferral Distance (Meters):", true, 29)]
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

        [EntryFloatUiProperty("GPS - Deferral Time (Mins.):", true, 30)]
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

        #endregion

        /// <summary>
        /// For JSON deserialization
        /// </summary>
        private Protocol()
        {
            _running = false;
            _forceProtocolReportsToRemoteDataStore = false;
            _lockPasswordHash = "";
            _jsonAnonymizer = new AnonymizedJsonContractResolver(this);
            _shareable = false;
            _pointsOfInterest = new List<PointOfInterest>();
            _participationHorizonDays = 1;   
            _groupable = false;
            _groupedProtocols = new List<Protocol>();
            _rewardThreshold = null;
            _gpsDesiredAccuracyMeters = GPS_DEFAULT_ACCURACY_METERS;
            _gpsMinTimeDelayMS = GPS_DEFAULT_MIN_TIME_DELAY_MS;
            _gpsMinDistanceDelayMeters = GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS;
        }

        /// <summary>
        /// Called by static CreateAsync. Should not be called directly by outside callers.
        /// </summary>
        /// <param name="name">Name.</param>
        private Protocol(string name)
            : this()
        {
            _name = name;

            ResetRandomTimeAnchor();            

            while (_storageDirectory == null)
            {
                _id = Guid.NewGuid().ToString();
                string candidateStorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), _id);
                if (!Directory.Exists(candidateStorageDirectory))
                {
                    _storageDirectory = candidateStorageDirectory;
                    Directory.CreateDirectory(_storageDirectory);
                }
            }

            _probes = new List<Probe>();
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

        private void ResetRandomTimeAnchor()
        {
            // pick a random time within the first 1000 years AD.
            _randomTimeAnchor = new DateTimeOffset((long)(new Random().NextDouble() * new DateTimeOffset(1000, 1, 1, 0, 0, 0, new TimeSpan()).Ticks), new TimeSpan());
        }

        public void Save(string path)
        {
            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] encryptedBytes = SensusServiceHelper.Encrypt(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS));
                file.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }

        public void CopyAsync(Action<Protocol> callback, bool useNewId)
        {
            new Thread(() =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                        {
                            Protocol copy = JsonConvert.DeserializeObject<Protocol>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

                            if (useNewId)
                                copy.Id = Guid.NewGuid().ToString();

                            callback(copy);
                        });

                }).Start();
        }

        public void StartAsync(Action callback = null)
        {
            new Thread(() =>
                {
                    Start();

                    if (callback != null)
                        callback();

                }).Start();
        }

        public void Start()
        {
            lock (_locker)
            {
                if (_running)
                    return;
                else
                    _running = true;

                if (ProtocolRunningChanged != null)
                    ProtocolRunningChanged(this, _running);

                SensusServiceHelper.Get().AddRunningProtocolId(_id);

                bool stopProtocol = false;

                // start local data store
                try
                {
                    if (_localDataStore == null)
                        throw new Exception("Local data store not defined.");
                        
                    _localDataStore.Start();

                    // start remote data store
                    try
                    {
                        if (_remoteDataStore == null)
                            throw new Exception("Remote data store not defined.");
                            
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
                                    if (probe.Enabled && probe is iOSHealthKitProbe)
                                        enabledHealthKitProbes.Add(probe as iOSHealthKitProbe);                                                                            

                                if (enabledHealthKitProbes.Count > 0)
                                {
                                    NSSet objectTypesToRead = NSSet.MakeNSObjectSet<HKObjectType>(enabledHealthKitProbes.Select(probe => probe.ObjectType).Distinct().ToArray());
                                    ManualResetEvent authorizationWait = new ManualResetEvent(false);
                                    new HKHealthStore().RequestAuthorizationToShare(new NSSet(), objectTypesToRead,
                                        (success, error) =>
                                        {
                                            if (error != null)
                                                SensusServiceHelper.Get().Logger.Log("Error while requesting HealthKit authorization:  " + error.Description, LoggingLevel.Normal, GetType());

                                            authorizationWait.Set();
                                        });

                                    authorizationWait.WaitOne();
                                }
                            }
                            #endif

                            SensusServiceHelper.Get().Logger.Log("Starting probes for protocol " + _name + ".", LoggingLevel.Normal, GetType());
                            int probesEnabled = 0;
                            int probesStarted = 0;
                            foreach (Probe probe in _probes)
                                if (probe.Enabled)
                                {
                                    ++probesEnabled;

                                    try
                                    {
                                        probe.Start();
                                        ++probesStarted;
                                    }
                                    catch (Exception)
                                    {                                        
                                    }
                                }

                            if (probesEnabled == 0)
                                throw new Exception("No probes were enabled.");
                            else if (probesStarted == 0)
                                throw new Exception("No probes started.");
                            else
                                SensusServiceHelper.Get().FlashNotificationAsync("Started \"" + _name + "\".");
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
                    Stop();
            }
        }

        public void StartWithUserAgreementAsync(string startupMessage, Action callback = null)
        {
            if (!_probes.Any(probe => probe.Enabled))
            {
                SensusServiceHelper.Get().FlashNotificationAsync("Probes not enabled. Cannot start.");

                if (callback != null)
                    callback();
        
                return;
            }

            int consentCode = new Random().Next(1000, 10000);

            StringBuilder collectionDescription = new StringBuilder();
            foreach (Probe probe in _probes.OrderBy(probe => probe.DisplayName))
                if (probe.Enabled && probe.StoreData)
                {
                    string probeCollectionDescription = probe.CollectionDescription;
                    if (!string.IsNullOrWhiteSpace(probeCollectionDescription))
                        collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + probeCollectionDescription);
                }

            List<Input> consent = new List<Input>();

            if (!string.IsNullOrWhiteSpace(startupMessage))
                consent.Add(new LabelOnlyInput(startupMessage));

            if (!string.IsNullOrWhiteSpace(_description))
                consent.Add(new LabelOnlyInput(_description));

            consent.Add(new LabelOnlyInput("This study would like to collect the following data from your device:"));

            LabelOnlyInput collectionDescriptionLabel = null;
            int collectionDescriptionFontSize = 15;
            if (collectionDescription.Length == 0)
                collectionDescriptionLabel = new LabelOnlyInput("No information will be collected.", collectionDescriptionFontSize);
            else
                collectionDescriptionLabel = new LabelOnlyInput(collectionDescription.ToString(), collectionDescriptionFontSize);

            collectionDescriptionLabel.Padding = new Thickness(20, 0, 0, 0);

            consent.Add(collectionDescriptionLabel);

            // the name in the following text input is used to grab the UI element when unit testing
            consent.Add(new SingleLineTextInput("ConsentCode", "To participate in this study as described above, please indicate your consent by entering the following code:  " + consentCode, Keyboard.Numeric)
                {
                    DisplayNumber = false
                });

            SensusServiceHelper.Get().PromptForInputsAsync(
                "Protocol Consent", 
                consent.ToArray(),
                null,
                true,
                null,
                "Are you sure you would like cancel your enrollment in this study?",
                null,
                null,
                false,
                inputs =>
                {
                    if (inputs != null)
                    {
                        string consentCodeStr = inputs.Last().Value as string;

                        int consentCodeInt;
                        if (int.TryParse(consentCodeStr, out consentCodeInt) && consentCodeInt == consentCode)
                            Start();
                        else
                            SensusServiceHelper.Get().FlashNotificationAsync("Incorrect code entered.");
                    }

                    if (callback != null)
                        callback();
                });
        }

        public void TestHealthAsync(bool userInitiated)
        {
            TestHealthAsync(userInitiated, () =>
                {
                });
        }

        public void TestHealthAsync(bool userInitiated, Action callback)
        {
            new Thread(() =>
                {
                    TestHealth(userInitiated);

                    if (callback != null)
                        callback();

                }).Start();
        }

        public void TestHealth(bool userInitiated)
        {
            lock (_locker)
            {
                string error = null;
                string warning = null;
                string misc = null;

                if (!_running)
                {
                    error += "Restarting protocol \"" + _name + "\"...";
                    try
                    {
                        Stop();
                        Start();
                    }
                    catch (Exception ex)
                    {
                        error += ex.Message + "...";
                    }

                    if (_running)
                        error += "restarted protocol." + Environment.NewLine;
                    else
                        error += "failed to restart protocol." + Environment.NewLine;
                }

                if (_running)
                {
                    if (_localDataStore == null)
                        error += "No local data store present on protocol." + Environment.NewLine;
                    else if (_localDataStore.TestHealth(ref error, ref warning, ref misc))
                    {
                        error += "Restarting local data store...";

                        try
                        {
                            _localDataStore.Restart();
                        }
                        catch (Exception ex)
                        {
                            error += ex.Message + "...";
                        }

                        if (!_localDataStore.Running)
                            error += "failed to restart local data store." + Environment.NewLine;
                    }

                    if (_remoteDataStore == null)
                        error += "No remote data store present on protocol." + Environment.NewLine;
                    else if (_remoteDataStore.TestHealth(ref error, ref warning, ref misc))
                    {
                        error += "Restarting remote data store...";

                        try
                        {
                            _remoteDataStore.Restart();
                        }
                        catch (Exception ex)
                        {
                            error += ex.Message + "...";
                        }

                        if (!_remoteDataStore.Running)
                            error += "failed to restart remote data store." + Environment.NewLine;
                    }

                    foreach (Probe probe in _probes)
                        if (probe.Enabled)
                        {
                            if (probe.TestHealth(ref error, ref warning, ref misc))
                            {
                                error += "Restarting probe \"" + probe.GetType().FullName + "\"...";

                                try
                                {
                                    probe.Restart();
                                }
                                catch (Exception ex)
                                {
                                    error += ex.Message + "...";
                                }

                                if (!probe.Running)
                                    error += "failed to restart probe \"" + probe.GetType().FullName + "\"." + Environment.NewLine;
                            }
                            else
                            {
                                // keep track of successful system-initiated health tests. this tells use how consistently the probe is running.
                                if (!userInitiated)
                                    lock (probe.SuccessfulHealthTestTimes)
                                    {
                                        probe.SuccessfulHealthTestTimes.Add(DateTime.Now);
                                        probe.SuccessfulHealthTestTimes.RemoveAll(healthTestTime => healthTestTime < ParticipationHorizon);
                                    }
                            }
                        }
                }

                _mostRecentReport = new ProtocolReportDatum(DateTimeOffset.UtcNow, error, warning, misc, this);
                SensusServiceHelper.Get().Logger.Log("Protocol report:" + Environment.NewLine + _mostRecentReport, LoggingLevel.Normal, GetType());

                SensusServiceHelper.Get().Logger.Log("Storing protocol report locally.", LoggingLevel.Normal, GetType());
                _localDataStore.AddNonProbeDatum(_mostRecentReport);

                if (!_localDataStore.UploadToRemoteDataStore && _forceProtocolReportsToRemoteDataStore)
                {
                    SensusServiceHelper.Get().Logger.Log("Local data aren't pushed to remote, so we're copying the report datum directly to the remote cache.", LoggingLevel.Normal, GetType());
                    _remoteDataStore.AddNonProbeDatum(_mostRecentReport);
                }
            }
        }

        public void ResetForSharing()
        {
            _randomTimeAnchor = DateTime.MinValue;
            _storageDirectory = null;
            _mostRecentReport = null;            

            foreach (Probe probe in _probes)
            {
                probe.ResetForSharing();

                // reset enabled status of probes to the original values. probes can be disabled when the protocol is started (e.g., if the user cancels out of facebook login.)
                probe.Enabled = probe.OriginallyEnabled;
            }

            if (_localDataStore != null)
                _localDataStore.ClearForSharing();

            if (_remoteDataStore != null)
                _remoteDataStore.ClearForSharing();
        }

        public void StopAsync(Action callback = null)
        {
            new Thread(() =>
                {
                    Stop();

                    if (callback != null)
                        callback();

                }).Start();
        }

        public void Stop()
        {
            lock (_locker)
            {
                if (_running)
                    _running = false;
                else
                    return;

                if (ProtocolRunningChanged != null)
                    ProtocolRunningChanged(this, _running);

                SensusServiceHelper.Get().RemoveRunningProtocolId(_id);

                SensusServiceHelper.Get().Logger.Log("Stopping protocol \"" + _name + "\".", LoggingLevel.Normal, GetType());

                foreach (Probe probe in _probes)
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

        public void DeleteAsync(Action callback = null)
        {
            new Thread(() =>
                {
                    Delete();

                    if (callback != null)
                        callback();
            
                }).Start();
        }

        public void Delete()
        {
            SensusServiceHelper.Get().UnregisterProtocol(this);

            try
            {
                Directory.Delete(StorageDirectory, true);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to delete protocol storage directory \"" + StorageDirectory + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
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
    }
}

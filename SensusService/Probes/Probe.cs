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
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SensusService.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    public abstract class Probe
    {
        #region static members

        public static void GetAllAsync(Action<List<Probe>> callback)
        {
            new Thread(() =>
                {
                    List<Probe> probes = null;
                    ManualResetEvent probesWait = new ManualResetEvent(false);

                    // the reflection stuff we do below (at least on android) needs to be run on the main thread.
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                        {
                            probes = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).OrderBy(p => p.DisplayName).ToList();
                            probesWait.Set();
                        });

                    probesWait.WaitOne();

                    callback(probes);

                }).Start();
        }

        #endregion

        /// <summary>
        /// Fired when the most recently sensed datum is changed.
        /// </summary>
        public event EventHandler<Tuple<Datum, Datum>> MostRecentDatumChanged;

        private bool _enabled;
        private bool _running;
        private HashSet<Datum> _collectedData;
        private Datum _mostRecentDatum;
        private Protocol _protocol;
        private bool _storeData;
        private DateTimeOffset _mostRecentStoreTimestamp;
        private bool _originallyEnabled;
        private DateTime? _startDateTime;
        private List<DateTime> _successfulHealthTestTimes;

        private readonly object _locker = new object();

        [JsonIgnore]
        public abstract string DisplayName { get; }

        [OnOffUiProperty("Enabled:", true, 2)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;

                    if (_protocol != null && _protocol.Running)  // _protocol can be null when deserializing the probe -- if Enabled is set before Protocol
                        if (_enabled)
                        StartAsync();
                    else
                        StopAsync();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether or not this probe was originally enabled within the protocol. Some probes can become disabled when 
        /// attempting to start them. For example, the temperature probe might not be supported on all hardware and will thus become 
        /// disabled after its failed initialization. Thus, we need a separate variable (other than Enabled) to tell us whether the 
        /// probe was originally enabled. We use this value to calculate participation levels and also to restore the probe before 
        /// sharing it with others (e.g., since other people might have temperature hardware in their devices).
        /// </summary>
        /// <value>Whether or not this probe was enabled the first time the protocol was started.</value>
        public bool OriginallyEnabled
        {
            get
            {
                return _originallyEnabled;
            }
            set
            {
                _originallyEnabled = value;
            }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        [JsonIgnore]
        public Datum MostRecentDatum
        {
            get { return _mostRecentDatum; }
            set
            {
                if (value != _mostRecentDatum)
                {
                    Datum previousDatum = _mostRecentDatum;

                    _mostRecentDatum = value;

                    if (MostRecentDatumChanged != null)
                        MostRecentDatumChanged(this, new Tuple<Datum, Datum>(previousDatum, _mostRecentDatum));
                }
            }
        }

        [JsonIgnore]
        public DateTimeOffset MostRecentStoreTimestamp
        {
            get{ return _mostRecentStoreTimestamp; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [OnOffUiProperty("Store Data:", true, 3)]
        public bool StoreData
        {
            get { return _storeData; }
            set { _storeData = value; }
        }

        [JsonIgnore]
        public abstract Type DatumType { get; }

        [JsonIgnore]
        protected abstract float RawParticipation { get; }

        [JsonIgnore]
        public DateTime? StartDateTime
        {
            get { return _startDateTime; }
        }

        public List<DateTime> SuccessfulHealthTestTimes
        {
            get{ return _successfulHealthTestTimes; }
        }

        protected Probe()
        {
            _enabled = _running = false;
            _storeData = true;
            _collectedData = new HashSet<Datum>();
            _successfulHealthTestTimes = new List<DateTime>();
        }

        /// <summary>
        /// Initializes this probe. Throws an exception if initialization fails.
        /// </summary>
        protected virtual void Initialize()
        {
            _collectedData.Clear();
            _mostRecentDatum = null;
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;  // mark storage delay from initialization of probe
        }

        /// <summary>
        /// Gets the participation level for the current probe. If this probe was originally enabled within the protocol, then
        /// this will be a value between 0 and 1, with 1 indicating perfect participation and 0 indicating no participation. If 
        /// this probe was not originally enabled within the protocol, then the returned value will be null, indicating that this
        /// probe should not be included in calculations of overall protocol participation.
        /// </summary>
        /// <returns>The participation level (null, or somewhere 0-1).</returns>
        public float? GetParticipation()
        {
            if (_originallyEnabled)
                return Math.Min(RawParticipation, 1);  // raw participations can be > 1, e.g. in the case of polling probes that the user can cause to poll repeatedly. cut off at 1 to maintain the interpretation of 1 as perfect participation.
            else
                return null;
        }

        protected void StartAsync()
        {
            new Thread(() =>
                {
                    try
                    {
                        Start();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to start:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                }).Start();
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails. Should be called first within child-class overrides.
        /// </summary>
        public virtual void Start()
        {
            lock (_locker)
            {
                if (_running)
                    SensusServiceHelper.Get().Logger.Log("Attempted to start probe, but it was already running.", LoggingLevel.Normal, GetType());
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());
                    Initialize();
                    _running = true;
                    _startDateTime = DateTime.Now;
                }
            }
        }

        public virtual void StoreDatum(Datum datum)
        {
            // datum is allowed to be null, indicating the the probe attempted to obtain data but it didn't find any (in the case of polling probes).
            if (datum != null)
            {
                datum.ProtocolId = Protocol.Id;

                if (_storeData)
                    lock (_collectedData)
                    {
                        SensusServiceHelper.Get().Logger.Log("Storing datum in cache.", LoggingLevel.Verbose, GetType());
                        _collectedData.Add(datum);
                    }
            }

            MostRecentDatum = datum;
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;  // this is outside the _storeData restriction above since we just want to track when this method is called.
        }

        public ICollection<Datum> GetCollectedData()
        {
            return _collectedData;
        }

        public void ClearDataCommittedToLocalDataStore(ICollection<Datum> data)
        {
            if (_collectedData != null)
                lock (_collectedData)
                {
                    int cleared = 0;
                    foreach (Datum datum in data)
                        if (_collectedData.Remove(datum))
                            ++cleared;

                    SensusServiceHelper.Get().Logger.Log("Cleared " + cleared + " committed data elements from cache.", LoggingLevel.Debug, GetType());
                }
        }

        protected void StopAsync()
        {
            new Thread(() =>
                {
                    try
                    {
                        Stop();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                }).Start();
        }

        /// <summary>
        /// Should be called first within child-class overrides.
        /// </summary>
        public virtual void Stop()
        {
            lock (_locker)
            {
                if (_running)
                {
                    SensusServiceHelper.Get().Logger.Log("Stopping.", LoggingLevel.Normal, GetType());

                    _running = false;
                    _startDateTime = null;

                    // clear out the probe's in-memory storage
                    lock (_collectedData)
                        _collectedData.Clear();
                }
                else
                    SensusServiceHelper.Get().Logger.Log("Attempted to stop probe, but it wasn't running.", LoggingLevel.Normal, GetType());
            }
        }

        public void Restart()
        {
            lock (_locker)
            {
                Stop();
                Start();
            }
        }

        public virtual bool TestHealth(ref string error, ref string warning, ref string misc)
        {                    
            bool restart = false;

            if (!_running)
            {
                restart = true;
                error += "Probe \"" + GetType().FullName + "\" is not running." + Environment.NewLine;
            }

            return restart;
        }

        public virtual void ResetForSharing()
        {
            if (_running)
                throw new Exception("Cannot clear probe while it is running.");
            
            _collectedData.Clear();
            _successfulHealthTestTimes.Clear();
            _mostRecentDatum = null;
            _mostRecentStoreTimestamp = DateTimeOffset.MinValue;
        }
    }
}
using Newtonsoft.Json;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SensusService.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    public abstract class Probe : INotifyPropertyChanged
    {
        #region static members
        /// <summary>
        /// Gets a list of all probes, uninitialized and with default parameter values.
        /// </summary>
        /// <returns></returns>
        public static List<Probe> GetAll()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).OrderBy(p => p.DisplayName).ToList();
        }
        #endregion

        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string _displayName;
        private bool _enabled;
        private bool _running;
        private HashSet<Datum> _collectedData;
        private Datum _mostRecentlyStoredDatum;
        private Protocol _protocol;

        [EntryStringUiProperty("Name:", true, 1)]
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (value != _displayName)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        [OnOffUiProperty("Enabled:", true, 2)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    OnPropertyChanged();

                    if (_protocol != null && _protocol.Running)  // _protocol can be null when deserializing the probe -- if Enabled is set before Protocol
                        if (_enabled)
                            StartAsync();
                        else
                            StopAsync();
                }
            }
        }


        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        [JsonIgnore]
        public Datum MostRecentlyStoredDatum
        {
            get { return _mostRecentlyStoredDatum; }
            set
            {
                if (value != _mostRecentlyStoredDatum)
                {
                    _mostRecentlyStoredDatum = value;
                    OnPropertyChanged();
                }
            }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        protected abstract string DefaultDisplayName { get; }

        protected Probe()
        {
            _displayName = DefaultDisplayName;
            _enabled = _running = false;
        }

        /// <summary>
        /// Initializes this probe. Throws an exception if initialization fails.
        /// </summary>
        protected virtual void Initialize()
        {
            _collectedData = new HashSet<Datum>();
        }

        protected Task StartAsync()
        {
            return Task.Run(() => Start());
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails.
        /// </summary>
        public virtual void Start()
        {
            lock (this)
            {
                if (_running)
                    SensusServiceHelper.Get().Logger.Log("Attempted to start probe \"" + GetType().FullName + "\", but it was already running.", LoggingLevel.Normal);
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Starting probe \"" + GetType().FullName + "\".", LoggingLevel.Normal);
                    Initialize();
                    _running = true;
                }
            }
        }

        protected virtual void StoreDatum(Datum datum)
        {
            if (datum != null)
                lock (_collectedData)
                {
                    SensusServiceHelper.Get().Logger.Log("Storing datum in probe cache:  " + datum, LoggingLevel.Debug);

                    _collectedData.Add(datum);

                    MostRecentlyStoredDatum = datum;
                }
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

                    SensusServiceHelper.Get().Logger.Log("Cleared " + cleared + " committed data elements from probe:  " + GetType().FullName, LoggingLevel.Verbose);
                }
        }

        protected Task StopAsync()
        {
            return Task.Run(() => Stop());
        }

        public virtual void Stop()
        {
            lock (this)
            {
                if (_running)
                {
                    SensusServiceHelper.Get().Logger.Log("Stopping probe \"" + GetType().FullName + "\".", LoggingLevel.Normal);
                    _running = false;

                    // clear out the probe's in-memory storage
                    lock (_collectedData)
                        _collectedData.Clear();
                }
                else
                    SensusServiceHelper.Get().Logger.Log("Attempted to stop probe \"" + GetType().FullName + "\", but it wasn't running.", LoggingLevel.Normal);
            }
        }

        public void Restart()
        {
            lock(this)
            {
                Stop();
                Start();
            }
        }

        public virtual bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                restart = true;
                error += "Probe \"" + GetType().FullName + "\" is not running." + Environment.NewLine;
            }

            return restart;
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
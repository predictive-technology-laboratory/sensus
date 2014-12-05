using Newtonsoft.Json;
using SensusService.Exceptions;
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
    public abstract class Probe : IProbe, INotifyPropertyChanged
    {
        #region static members
        static Probe()
        {
            Probe[] probes = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).ToArray();
            if (probes.Length != probes.Select(p => p.Id).Distinct().Count())
                throw new SensusException("All probes must have distinct IDs.");
        }

        /// <summary>
        /// Gets a list of all probes, uninitialized and with default parameter values.
        /// </summary>
        /// <returns></returns>
        public static List<Probe> GetAll()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).ToList();
        }
        #endregion

        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string _displayName;
        private bool _enabled;
        private HashSet<Datum> _collectedData;
        private Protocol _protocol;
        private ProbeController _controller;
        private bool _supported;
        private Datum _mostRecentlyStoredDatum;

        [EntryStringUiProperty("Name:", true)]
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (!value.Equals(_displayName, StringComparison.Ordinal))
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        [OnOffUiProperty("Enabled:", true)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    OnPropertyChanged();

                    if (_protocol != null && _protocol.Running)  // _protocol can be null when deserializing the probe
                        if (_enabled)
                            InitializeAndStartAsync();
                        else
                            _controller.StopAsync();
                }
            }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        public ProbeController Controller
        {
            get { return _controller; }
            set
            {
                if (value != _controller)
                {
                    bool previousRunningValue = _controller != null && _controller.Running;

                    _controller = value;

                    if (previousRunningValue != _controller.Running)
                        OnPropertyChanged("Running");  // the running status of probes comes from the controller, so if the controller changes we should update
                }
            }
        }

        public bool Supported
        {
            get { return _supported; }
            set
            {
                if (value != _supported)
                {
                    _supported = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public Datum MostRecentlyStoredDatum
        {
            get { return _mostRecentlyStoredDatum; }
            set
            {
                if(value != _mostRecentlyStoredDatum)
                {
                    _mostRecentlyStoredDatum = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _controller != null && _controller.Running; }  // can be null if this property is referenced by another when deserializing
        }

        protected abstract int Id { get; }

        protected abstract string DefaultDisplayName { get; }

        protected abstract ProbeController DefaultController { get; }

        protected Probe()
        {
            _displayName = DefaultDisplayName;
            _enabled = false;
            _supported = true;
            _controller = DefaultController;
        }

        protected virtual bool Initialize()
        {
            _collectedData = new HashSet<Datum>();

            return _supported;
        }

        public Task<bool> InitializeAndStartAsync()
        {
            return Task.Run<bool>(async () =>
                {
                    try
                    {
                        if (Initialize())
                            await _controller.StartAsync();
                    }
                    catch (Exception ex) { if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Normal) SensusServiceHelper.Get().Log("Failed to start probe \"" + DisplayName + "\":" + ex.Message + Environment.NewLine + ex.StackTrace); }

                    return _controller.Running;
                });
        }

        public virtual void StoreDatum(Datum datum)
        {
            if (datum != null)
                lock (_collectedData)
                {
                    if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Debug)
                        SensusServiceHelper.Get().Log("Storing datum in probe cache:  " + datum);

                    _collectedData.Add(datum);

                    MostRecentlyStoredDatum = datum;
                }
        }

        public ICollection<Datum> GetCollectedData()
        {
            return _collectedData;
        }

        public void ClearCommittedData(ICollection<Datum> data)
        {
            if (_collectedData != null)
                lock (_collectedData)
                {
                    int removed = 0;
                    foreach (Datum datum in data)
                        if (_collectedData.Remove(datum))
                            ++removed;

                    if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                        SensusServiceHelper.Get().Log("Cleared " + removed + " committed data elements from probe:  " + _displayName);
                }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
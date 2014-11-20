using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sensus.UI.Properties;
using System.Threading.Tasks;

namespace Sensus.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    [Serializable]
    public abstract class Probe : IProbe, INotifyPropertyChanged
    {
        #region static members
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
        [NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        private string _name;
        private bool _enabled;
        [NonSerialized]
        private HashSet<Datum> _collectedData;
        [NonSerialized]
        private Protocol _protocol;
        private ProbeController _controller;
        private bool _supported;

        public int Id
        {
            get { return _id; }
        }

        [StringUiProperty("Name:", true)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [BooleanUiProperty("Enabled:", true)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    OnPropertyChanged();

                    if (_protocol.Running)
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
                    bool previousRunningValue = _controller.Running;

                    _controller = value;

                    if (previousRunningValue != _controller.Running)
                        OnPropertyChanged("Running");  // the running status of probes come from the controller, so if the controller changes we should update
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

        public bool Running
        {
            get { return _controller.Running; }
        }

        protected abstract string DisplayName { get; }

        public Probe()
        {
            _id = -1;  // TODO:  Get reasonable probe ID
            _name = DisplayName;
            _enabled = false;
            _supported = true;

            if (this is ActivePassiveProbe)
            {
                ActivePassiveProbe probe = this as ActivePassiveProbe;
                if (probe.Passive)
                    _controller = new PassiveProbeController(probe);
                else
                    _controller = new ActiveProbeController(probe);
            }
            else if (this is IActiveProbe)
                _controller = new ActiveProbeController(this as IActiveProbe);
            else if (this is IPassiveProbe)
                _controller = new PassiveProbeController(this as IPassiveProbe);
            else
                throw new ProbeException(this, "Could not find controller for probe " + _name + " (" + GetType().FullName + ").");
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
                    catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to start probe \"" + Name + "\":" + ex.Message + Environment.NewLine + ex.StackTrace); }

                    return _controller.Running;
                });
        }

        public virtual void StoreDatum(Datum datum)
        {
            if (datum != null)
                lock (_collectedData)
                {
                    if (App.LoggingLevel >= LoggingLevel.Debug)
                        App.Get().SensusService.Log("Storing datum in probe cache:  " + datum);

                    _collectedData.Add(datum);
                }
        }

        public ICollection<Datum> GetCollectedData()
        {
            return _collectedData;
        }

        public void ClearCommittedData(ICollection<Datum> data)
        {
            lock (_collectedData)
            {
                if (App.LoggingLevel >= LoggingLevel.Verbose)
                    App.Get().SensusService.Log("Clearing committed data from probe cache:  " + data.Count + " items.");

                foreach (Datum datum in data)
                    _collectedData.Remove(datum);
            }
        }

        public void DeserializationRebind()
        {
            _controller.Probe = this;
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
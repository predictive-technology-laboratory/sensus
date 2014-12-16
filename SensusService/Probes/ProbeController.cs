using Newtonsoft.Json;
using SensusService.Exceptions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SensusService.Probes
{
    public abstract class ProbeController : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private IProbe _probe;
        private bool _running;

        public IProbe Probe
        {
            get { return _probe; }
            set { _probe = value; }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
            private set
            {
                lock (this)
                {
                    if (value == _running)
                        throw new ProbeControllerException(this, "Attempted to " + (value ? "start" : "stop") + " controller, but it was already " + (value ? "started" : "stopped"));

                    _running = value;

                    OnPropertyChanged();
                    _probe.OnPropertyChanged("Running");  // controllers hold the running state of probes, so notify watchers of the associated probe that this controller's running status has changed
                }
            }
        }

        protected ProbeController(IProbe probe)
        {
            _probe = probe;
            _running = false;
        }

        public virtual void Start()
        {
            SensusServiceHelper.Get().Logger.Log("Starting " + GetType().FullName + " for " + _probe.DisplayName, LoggingLevel.Normal);
            Running = true;
        }

        public virtual void Stop()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping " + GetType().FullName + " for " + _probe.DisplayName, LoggingLevel.Normal);
            Running = false;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

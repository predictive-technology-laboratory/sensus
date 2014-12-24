using Newtonsoft.Json;
using SensusService.Exceptions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SensusService.Probes
{
    public abstract class ProbeController
    {
        private Probe _probe;
        private bool _running;

        public Probe Probe
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

                    _probe.OnPropertyChanged("Running");  // controllers hold the running state of probes, so notify watchers of the associated probe that this controller's running status has changed
                }
            }
        }

        protected ProbeController(Probe probe)
        {
            _probe = probe;
            _running = false;
        }

        public void StartAsync()
        {
            Task.Run(() => { Start(); });
        }

        public virtual void Start()
        {
            lock (this)
            {
                SensusServiceHelper.Get().Logger.Log("Starting " + GetType().FullName + " for " + _probe.DisplayName, LoggingLevel.Normal);
                Running = true;
            }
        }

        public void StopAsync()
        {
            Task.Run(() => { Stop(); });
        }

        public virtual void Stop()
        {
            lock (this)
            {
                SensusServiceHelper.Get().Logger.Log("Stopping " + GetType().FullName + " for " + _probe.DisplayName, LoggingLevel.Normal);
                Running = false;
            }
        }

        public virtual bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (_probe.Protocol.Running && _probe.Enabled && !_running)
            {
                error += "Controller for enabled probe \"" + _probe.DisplayName + "\" is not running.";
                restart = true;
            }

            return restart;
        }
    }
}

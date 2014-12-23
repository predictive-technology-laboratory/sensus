using Newtonsoft.Json;
using SensusService.Exceptions;
using System.ComponentModel;

namespace SensusService.Probes
{
    public abstract class ProbeController
    {
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

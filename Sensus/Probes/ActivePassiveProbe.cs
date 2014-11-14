using Sensus.Exceptions;
using Sensus.UI.Properties;

namespace Sensus.Probes
{
    public abstract class ActivePassiveProbe : ActiveProbe
    {
        private bool _passive;

        [BooleanUiProperty("Passive:", true)]
        public bool Passive
        {
            get { return _passive; }
            set
            {
                if(value != _passive)
                {
                    _passive = value;
                    OnPropertyChanged();
                }
            }
        }

        public override void Start()
        {
            if (_passive)
            {
                ChangeState(ProbeState.Initialized, ProbeState.Starting);
                StartListening();
                ChangeState(ProbeState.Starting, ProbeState.Started);
            }
            else
                base.Start();
        }

        public override void Stop()
        {
            if (_passive)
            {
                ChangeState(ProbeState.Started, ProbeState.Stopping);
                StopListening();
                ChangeState(ProbeState.Stopping, ProbeState.Stopped);
            }
            else
                base.Stop();
        }

        protected abstract void StartListening();

        protected abstract void StopListening();
    }
}

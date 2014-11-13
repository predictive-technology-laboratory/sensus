using Sensus.Probes.Parameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    public abstract class ActivePassiveProbe : ActiveProbe
    {
        private bool _passive;

        [BooleanProbeParameter("Passive:", true)]
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
                StartListening();
            else
                base.Start();
        }

        public override void Stop()
        {
            if (_passive)
                StopListening();
            else
                base.Stop();
        }

        protected abstract void StartListening();

        protected abstract void StopListening();
    }
}

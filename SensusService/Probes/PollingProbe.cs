using System.Collections.Generic;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe, IPollingProbe
    {
        protected override ProbeController DefaultController
        {
            get { return new PollingProbeController(this); }
        }

        public abstract IEnumerable<Datum> Poll();
    }
}

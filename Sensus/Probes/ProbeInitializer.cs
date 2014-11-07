using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    /// <summary>
    /// Initializes probes with platform-appropriate bindings.
    /// </summary>
    public class ProbeInitializer
    {
        public virtual ProbeState Initialize(Probe probe)
        {
            probe.State = ProbeState.Initializing;

            return probe.State;
        }
    }
}

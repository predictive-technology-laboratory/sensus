using Sensus.Exceptions;
using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    /// <summary>
    /// Initializes protocols and their probes with platform-generic bindings.
    /// </summary>
    public class ProbeInitializer
    {
        public void Initialize(Protocol protocol)
        {
            foreach (Probe probe in protocol.Probes)
            {
                if (Initialize(probe) == ProbeState.Initialized)
                {
                    try { probe.Test(); }
                    catch (ProbeTestException e)
                    {
                        probe.State = ProbeState.TestFailed;
                        Console.Error.WriteLine(e.Message);
                    }
                }
            }
        }

        protected virtual ProbeState Initialize(Probe probe)
        {
            probe.State = ProbeState.Initializing;

            return probe.State;
        }
    }
}

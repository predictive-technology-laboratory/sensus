using System;
using System.Collections.Generic;

namespace Sensus.Probes
{
    /// <summary>
    /// Initializes protocols and their probes with platform-generic bindings.
    /// </summary>
    public class ProbeInitializer
    {
        public void InitializeProbes(List<Probe> probes)
        {
            // initialize all probes, so that they can be enabled/started after the protocol has begun
            foreach (Probe probe in probes)
            {
                try { Initialize(probe); }
                catch (Exception ex)
                {
                    if (Logger.Level >= LoggingLevel.Normal)
                        Logger.Log("Probe \"" + probe.Name + "\" failed to initialize:  " + ex.Message);

                    probe.ChangeState(ProbeState.Initializing, ProbeState.InitializeFailed);
                }
            }
        }

        public void InitializeProbe(Probe probe)
        {
            InitializeProbes(new List<Probe>(new Probe[] { probe }));
        }

        protected virtual ProbeState Initialize(Probe probe)
        {
            probe.Initialize();

            return probe.State;
        }
    }
}

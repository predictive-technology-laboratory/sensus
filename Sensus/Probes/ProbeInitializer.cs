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
        #region static members
        private static ProbeInitializer _probeInitializer;

        public static ProbeInitializer Get()
        {
            return _probeInitializer;
        }

        public static void Set(ProbeInitializer probeInitializer)
        {
            _probeInitializer = probeInitializer;
        }
        #endregion

        public void Initialize(List<Probe> probes)
        {
            foreach (Probe probe in probes)
                if (Initialize(probe) == ProbeState.Initialized)
                {
                    try { probe.Test(); }
                    catch (ProbeTestException ex)
                    {
                        Console.Error.WriteLine("Test failed for probe \"" + probe.Name + "\":  " + ex.Message);
                        probe.State = ProbeState.TestFailed;
                    }
                }
        }

        protected virtual ProbeState Initialize(Probe probe)
        {
            probe.Initialize();

            return probe.State;
        }
    }
}

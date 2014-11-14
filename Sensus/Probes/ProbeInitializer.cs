using Sensus.Exceptions;
using System;
using System.Collections.Generic;

namespace Sensus.Probes
{
    /// <summary>
    /// Initializes protocols and their probes with platform-generic bindings.
    /// </summary>
    public class ProbeInitializer
    {
        public void Initialize(List<Probe> probes)
        {
            foreach (Probe probe in probes)
                if (probe.Enabled)
                {
                    try { Initialize(probe); }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Probe \"" + probe.Name + "\" failed to initialize:  " + ex.Message);
                        probe.ChangeState(ProbeState.Initializing, ProbeState.InitializeFailed);
                    }

                    if (probe.State == ProbeState.Initialized)
                    {
                        try
                        {
                            Console.Error.WriteLine("Testing probe \"" + probe.Name + "\".");

                            probe.Test();
                            probe.ChangeState(ProbeState.Initialized, ProbeState.TestPassed);

                            Console.Error.WriteLine("Probe \"" + probe.Name + "\" passed its self-test.");
                        }
                        catch (ProbeTestException ex)
                        {
                            Console.Error.WriteLine("Probe \"" + probe.Name + "\" failed its self-test:  " + ex.Message);
                            probe.ChangeState(ProbeState.Initialized, ProbeState.TestFailed);
                        }
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

using System;
using System.Collections.Generic;
using System.Text;
using Sensus.Exceptions;
using Sensus.Probes;

namespace Sensus.Protocols
{
    /// <summary>
    /// Manages the execution of a protocol.
    /// </summary>
    public class ProtocolController
    {
        private Protocol _protocol;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="protocol">Protocol to execute.</param>
        /// <param name="initializer">Initializer for probes.</param>
        public ProtocolController(Protocol protocol, ProbeInitializer initializer)
        {
            _protocol = protocol;

            foreach (Probe probe in _protocol.Probes)
            {
                if (initializer.Initialize(probe) == ProbeState.Initialized)
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

        public void ExecuteProtocol()
        {
            foreach (Probe probe in _protocol.Probes)
                if (probe.State == ProbeState.Initialized)
                    probe.StartPolling();
        }

        public void HaltProtocol()
        {
            foreach (Probe probe in _protocol.Probes)
                if (probe.State == ProbeState.Polling)
                    probe.StopPolling();
        }
    }
}

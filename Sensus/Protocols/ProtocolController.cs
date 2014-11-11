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
        public ProtocolController(Protocol protocol)
        {
            _protocol = protocol;
        }

        public void ExecuteProtocol()
        {
            if (!_protocol.Running)
            {
                _protocol.Running = true;
                foreach (Probe probe in _protocol.Probes)
                    if (probe.State == ProbeState.Initialized)
                        probe.StartPolling();
            }
        }

        public void HaltProtocol()
        {
            if (_protocol.Running)
            {
                foreach (Probe probe in _protocol.Probes)
                    if (probe.State == ProbeState.Polling)
                        probe.StopPolling();

                _protocol.Running = false;
            }
        }
    }
}

using Sensus.Probes;
using System;

namespace Sensus.Exceptions
{
    /// <summary>
    /// General exception for probes.
    /// </summary>
    public class ProbeException : Exception
    {
        private Probe _probe;

        public ProbeException(Probe probe, string message)
            : base(message)
        {
            _probe = probe;
        }
    }
}

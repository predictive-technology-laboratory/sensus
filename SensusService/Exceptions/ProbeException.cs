using SensusService.Probes;

namespace SensusService.Exceptions
{
    /// <summary>
    /// General exception for probes.
    /// </summary>
    public class ProbeException : SensusException
    {
        private Probe _probe;

        public ProbeException(Probe probe, string message)
            : base(message)
        {
            _probe = probe;
        }
    }
}

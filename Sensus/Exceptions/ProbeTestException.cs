using Sensus.Probes;

namespace Sensus.Exceptions
{
    public class ProbeTestException : ProbeException
    {
        public ProbeTestException(Probe probe, string error)
            : base(probe, "Probe test failed:  " + error)
        {

        }
    }
}

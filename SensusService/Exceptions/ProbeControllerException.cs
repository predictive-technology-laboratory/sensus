using SensusService.Probes;

namespace SensusService.Exceptions
{
    public class ProbeControllerException : SensusException
    {
        public ProbeController _controller;

        public ProbeControllerException(ProbeController controller, string message)
            : base(message)
        {
            _controller = controller;
        }
    }
}

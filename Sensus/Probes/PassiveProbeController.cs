using Sensus.Exceptions;
namespace Sensus.Probes
{
    public class PassiveProbeController : ProbeController
    {
        public PassiveProbeController(IPassiveProbe probe)
            : base(probe)
        {
        }

        public override void StartAsync()
        {
            base.StartAsync();

            (Probe as IPassiveProbe).StartListening();
        }

        public override void StopAsync()
        {
            base.StopAsync();

            (Probe as IPassiveProbe).StopListening();
        }
    }
}

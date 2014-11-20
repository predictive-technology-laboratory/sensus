using Sensus.Exceptions;
using System;
using System.Threading.Tasks;
namespace Sensus.Probes
{
    [Serializable]
    public class PassiveProbeController : ProbeController
    {
        public PassiveProbeController(IPassiveProbe probe)
            : base(probe)
        {
        }

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StartAsync();

                    (Probe as IPassiveProbe).StartListening();
                });
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StopAsync();

                    (Probe as IPassiveProbe).StopListening();
                });
        }
    }
}

using System.Threading.Tasks;

namespace SensusService.Probes
{
    public class ListeningProbeController : ProbeController
    {
        public ListeningProbeController(IListeningProbe probe)
            : base(probe)
        {
        }

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StartAsync();

                    (Probe as IListeningProbe).StartListening();
                });
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StopAsync();

                    (Probe as IListeningProbe).StopListening();
                });
        }
    }
}

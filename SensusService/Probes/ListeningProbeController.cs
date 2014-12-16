using System.Threading.Tasks;

namespace SensusService.Probes
{
    public class ListeningProbeController : ProbeController
    {
        public ListeningProbeController(IListeningProbe probe)
            : base(probe)
        {
        }

        public override void Start()
        {
            base.Start();

            (Probe as IListeningProbe).StartListening();
        }

        public override void Stop()
        {
            base.Stop();

            (Probe as IListeningProbe).StopListening();
        }
    }
}

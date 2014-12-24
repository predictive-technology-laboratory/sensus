
namespace SensusService.Probes
{
    public class ListeningProbeController : ProbeController
    {
        public ListeningProbeController(ListeningProbe probe)
            : base(probe)
        {
        }

        public override void Start()
        {
            lock (this)
            {
                base.Start();

                (Probe as ListeningProbe).StartListening();
            }
        }

        public override void Stop()
        {
            lock (this)
            {
                base.Stop();

                (Probe as ListeningProbe).StopListening();
            }
        }
    }
}

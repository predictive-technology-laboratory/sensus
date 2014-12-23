
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
            lock (this)
            {
                base.Start();

                (Probe as IListeningProbe).StartListening();
            }
        }

        public override void Stop()
        {
            lock (this)
            {
                base.Stop();

                (Probe as IListeningProbe).StopListening();
            }
        }
    }
}

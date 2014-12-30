
namespace SensusService.Probes.Device
{
    /// <summary>
    /// Probes information about screen on/off status.
    /// </summary>
    public abstract class ScreenProbe : PollingProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Screen"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 10000; }
        }
    }
}

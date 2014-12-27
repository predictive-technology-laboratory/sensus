
namespace SensusService.Probes.Device
{
    /// <summary>
    /// Probes information about the battery.
    /// </summary>
    public abstract class BatteryProbe : PollingProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Battery"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 60000; }
        }
    }
}

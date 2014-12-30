namespace SensusService.Probes.Context
{
    public abstract class SoundProbe : PollingProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Sound"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 10000; }
        }
    }
}

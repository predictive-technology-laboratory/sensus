using SensusUI.UiProperties;
using System.Collections.Generic;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        protected sealed override ProbeController DefaultController
        {
            get { return new PollingProbeController(this); }
        }

        public abstract int DefaultPollingSleepDurationMS { get; }

        [EntryIntegerUiProperty("Sleep Duration:", true, 5)]
        public int PollingSleepDurationMS
        {
            get
            {
                if (Controller is PollingProbeController)
                    return (Controller as PollingProbeController).SleepDurationMS;
                else
                    return -1;
            }
            set
            {
                if (Controller is PollingProbeController)
                {
                    PollingProbeController pollingController = Controller as PollingProbeController;
                    if (pollingController.SleepDurationMS != value)
                    {
                        (Controller as PollingProbeController).SleepDurationMS = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        public abstract IEnumerable<Datum> Poll();
    }
}

using System.Collections.Generic;

namespace SensusService.Probes.Interactive
{
    public class QuestionProbe : PollingProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Questions"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 10000; }
        }

        protected override IEnumerable<Datum> Poll()
        {
            return new Datum[] { };
        }
    }
}

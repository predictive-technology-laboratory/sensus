
using System.Collections.Generic;

namespace SensusService.Probes
{
    public interface IPollingProbe : IProbe
    {
        int DefaultPollingSleepDurationMS { get; }

        IEnumerable<Datum> Poll();
    }
}

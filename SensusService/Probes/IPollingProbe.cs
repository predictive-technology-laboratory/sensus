
using System.Collections.Generic;

namespace SensusService.Probes
{
    public interface IPollingProbe : IProbe
    {
        IEnumerable<Datum> Poll();
    }
}

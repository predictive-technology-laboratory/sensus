
namespace SensusService.Probes
{
    public interface IPollingProbe : IProbe
    {
        Datum Poll();
    }
}

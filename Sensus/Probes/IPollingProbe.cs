namespace Sensus.Probes
{
    public interface IPollingProbe : IProbe
    {
        Datum Poll();
    }
}

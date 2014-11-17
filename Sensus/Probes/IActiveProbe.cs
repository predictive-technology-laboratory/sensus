namespace Sensus.Probes
{
    public interface IActiveProbe : IProbe
    {
        Datum Poll();
    }
}

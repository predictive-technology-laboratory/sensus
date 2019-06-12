namespace Sensus.Probes.Device
{
    public interface IBatteryDatum : IDatum
    {
        double Level { get; set; }
    }
}
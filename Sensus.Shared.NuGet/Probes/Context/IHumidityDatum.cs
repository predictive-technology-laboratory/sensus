namespace Sensus.Probes.Context
{
    public interface IHumidityDatum : IDatum
    {
        double RelativeHumidity { get; set; }
    }
}
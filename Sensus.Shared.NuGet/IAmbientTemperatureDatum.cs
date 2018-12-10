namespace Sensus.Probes.Context
{
    public interface IAmbientTemperatureDatum : IDatum
    {
        double DegreesCelsius { get; set; }
    }
}
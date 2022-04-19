namespace Sensus.Probes.Context
{
    public interface ILightDatum : IDatum
    {
        double Brightness { get; set; }
    }
}
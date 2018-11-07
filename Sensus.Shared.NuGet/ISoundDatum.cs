namespace Sensus.Probes.Context
{
    public interface ISoundDatum : IDatum
    {
        double Decibels { get; set; }
    }
}
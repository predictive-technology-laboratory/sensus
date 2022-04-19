namespace Sensus.Probes.Location
{
    public interface IProximityDatum : IDatum
    {
        double Distance { get; set; }
        double MaxDistance { get; set; }
    }
}
namespace Sensus.Probes.Location
{
    public interface ILocationDatum : IDatum
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
    }
}
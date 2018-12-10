namespace Sensus.Probes.Location
{
    public interface ICompassDatum : IDatum
    {
        double Heading { get; set; }
    }
}
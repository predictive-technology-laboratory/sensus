namespace Sensus.Probes.Location
{
    public interface IAltitudeDatum : IDatum
    {
        double Altitude { get; set; }
    }
}
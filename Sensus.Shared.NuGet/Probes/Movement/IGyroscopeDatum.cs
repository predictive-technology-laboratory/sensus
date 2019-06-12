namespace Sensus.Probes.Movement
{
    public interface IGyroscopeDatum : IDatum
    {
        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }
    }
}
namespace Sensus.Probes.Device
{
    public interface IScreenDatum : IDatum
    {
        bool On { get; set; }
    }
}
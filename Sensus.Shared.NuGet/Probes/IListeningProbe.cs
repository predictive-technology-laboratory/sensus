namespace Sensus.Probes
{
    public interface IListeningProbe : IProbe
    {
        double? MaxDataStoresPerSecond { get; set; }
        bool KeepDeviceAwake { get; set; }
    }
}
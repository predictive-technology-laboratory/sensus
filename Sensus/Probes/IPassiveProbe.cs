namespace Sensus.Probes
{
    public interface IPassiveProbe : IProbe
    {
        int MaxDataStoresPerSecond { get; set; }

        void StartListening();

        void StopListening();
    }
}

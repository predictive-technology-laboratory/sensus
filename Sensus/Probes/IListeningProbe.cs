namespace Sensus.Probes
{
    public interface IListeningProbe : IProbe
    {
        int MaxDataStoresPerSecond { get; set; }

        void StartListening();

        void StopListening();
    }
}

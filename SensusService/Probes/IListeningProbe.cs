
namespace SensusService.Probes
{
    public interface IListeningProbe : IProbe
    {
        void StartListening();

        void StopListening();
    }
}

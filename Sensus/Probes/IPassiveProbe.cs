namespace Sensus.Probes
{
    public interface IPassiveProbe : IProbe
    {
        int Throttle { get; set; }

        void StartListening();

        void StopListening();
    }
}

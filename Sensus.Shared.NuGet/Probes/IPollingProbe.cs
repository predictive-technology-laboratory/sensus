namespace Sensus.Probes
{
    public interface IPollingProbe : IProbe
    {
        int PollingSleepDurationMS { get; set; }
    }
}
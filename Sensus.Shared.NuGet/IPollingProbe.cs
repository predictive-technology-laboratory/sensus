namespace Sensus.Probes
{
    public interface IPollingProbe
    {
        int PollingSleepDurationMS { get; set; }
    }
}
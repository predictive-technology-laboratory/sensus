namespace Sensus.Probes.Movement
{
    public interface IActivityDatum : IDatum
    {
        Activities Activity { get; set; }
        ActivityPhase Phase { get; set; }
        ActivityState State { get; set; }
        ActivityConfidence Confidence { get; set; }
        Activities ActivityStarting { get; }
        Activities ActivityContinuing { get; }
        Activities ActivityStopping { get; }
    }
}
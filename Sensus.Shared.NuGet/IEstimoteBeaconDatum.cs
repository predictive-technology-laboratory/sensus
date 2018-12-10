namespace Sensus.Probes.Location
{
    public interface IEstimoteBeaconDatum : IDatum
    {
        string BeaconTag { get; set; }
        string EventName { get; set; }
        double DistanceMeters { get; set; }
        EstimoteBeaconProximityEvent ProximityEvent { get; set; }
        string EventSummary { get; }
    }
}
namespace Sensus.Probes.Location
{
    public interface IPointOfInterestProximityDatum : IDatum
    {
        string PoiName { get; set; }
        string PoiType { get; set; }
        double PoiLatitude { get; set; }
        double PoiLongitude { get; set; }
        double DistanceToPoiMeters { get; set; }
        double TriggerDistanceMeters { get; set; }
        ProximityThresholdDirection TriggerDistanceDirection { get; set; }
    }
}
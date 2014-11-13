using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Probes location information.
    /// </summary>
    public class LocationProbe : GpsProbe
    {
        protected override string DisplayName
        {
            get { return "Location"; }
        }

        protected override Datum ConvertReadingToDatum(Position reading)
        {
            return new LocationDatum(Id, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude);
        }

        protected override void StartListening()
        {
            GpsReceiver.StartListeningForChanges(MinimumTimeHint, MinimumDistanceHint, false);
        }
    }
}

using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Probes information about the device's orientation relative to magnetic north.
    /// </summary>
    public class CompassProbe : GpsProbe
    {
        protected override string DisplayName
        {
            get { return "Compass"; }
        }

        protected override Datum ConvertReadingToDatum(Position reading)
        {
            return new CompassDatum(Id, reading.Timestamp, reading.Heading);
        }

        protected override void StartListening()
        {
            GpsReceiver.StartListeningForChanges(MinimumTimeHint, MinimumDistanceHint, true);
        }
    }
}

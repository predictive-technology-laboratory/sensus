using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    public class AltitudeProbe : GpsProbe
    {
        protected override string DisplayName
        {
            get { return "Altitude"; }
        }

        protected override Datum ConvertReadingToDatum(Position reading)
        {
            return new AltitudeDatum(Id, reading.Timestamp, reading.AltitudeAccuracy, reading.Altitude);
        }

        protected override void StartListening()
        {
            GpsReceiver.StartListeningForChanges(MinimumTimeHint, MinimumDistanceHint, false);
        }
    }
}

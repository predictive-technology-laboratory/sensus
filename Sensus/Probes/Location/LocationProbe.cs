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

        public LocationProbe()
        {
            if (!GpsReceiver.Get().Locator.IsGeolocationEnabled)
                Supported = false;
        }

        protected override Datum ConvertReadingToDatum(Position reading)
        {
            if (reading == null)
                return null;

            return new LocationDatum(Id, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude);
        }
    }
}

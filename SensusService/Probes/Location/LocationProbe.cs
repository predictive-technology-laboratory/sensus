using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// Probes location information.
    /// </summary>
    public class LocationProbe : GpsProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Location"; }
        }

        protected override bool Initialize()
        {
            return base.Initialize() && GpsReceiver.Get().Locator.IsGeolocationEnabled;
        }

        protected override Datum ConvertReadingToDatum(Position reading)
        {
            if (reading == null)
                return null;

            return new LocationDatum(this, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude);
        }
    }
}

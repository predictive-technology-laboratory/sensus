using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// Probes location information.
    /// </summary>
    public class LocationProbe : GpsProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Location"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 1000 * 10; }
        }

        protected override bool Initialize()
        {
            return base.Initialize() && GpsReceiver.Get().Locator.IsGeolocationEnabled;
        }

        protected sealed override Datum ConvertReadingToDatum(Position reading)
        {
            if (reading == null)
                return null;

            return new LocationDatum(this, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude);
        }
    }
}

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
            if (reading == null)
                return null;

            return new LocationDatum(Id, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude);
        }

        public override ProbeState Initialize()
        {
            base.Initialize();

            if (GpsReceiver.Get().Locator.IsGeolocationEnabled)
                ChangeState(ProbeState.Initializing, ProbeState.Initialized);
            else
                ChangeState(ProbeState.Initializing, ProbeState.Unsupported);

            return State;
        }
    }
}

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
            if (reading == null)
                return null;

            return new CompassDatum(Id, reading.Timestamp, reading.Heading);
        }

        public override ProbeState Initialize()
        {
            base.Initialize();

            if (GpsReceiver.Get().Locator.IsGeolocationEnabled && GpsReceiver.Get().Locator.SupportsHeading)
                ChangeState(ProbeState.Initializing, ProbeState.Initialized);
            else
                ChangeState(ProbeState.Initializing, ProbeState.Unsupported);

            return State;
        }
    }
}

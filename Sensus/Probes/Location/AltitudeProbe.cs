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
            if (reading == null)
                return null;

            return new AltitudeDatum(Id, reading.Timestamp, reading.AltitudeAccuracy, reading.Altitude);
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

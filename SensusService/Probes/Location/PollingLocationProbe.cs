using System.Collections.Generic;
using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// Probes location information via polling.
    /// </summary>
    public class PollingLocationProbe : PollingProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Location (Polling)"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 1000 * 10; }
        }

        protected override bool Initialize()
        {
            return base.Initialize() && GpsReceiver.Get().Locator.IsGeolocationEnabled;
        }

        public sealed override IEnumerable<Datum> Poll()
        {
            Position reading = GpsReceiver.Get().GetReading((Controller as PollingProbeController).SleepDurationMS, 30000);

            if (reading == null)
                return new Datum[] { };
            else
                return new Datum[] { new LocationDatum(this, reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude) };
        }
    }
}

using System;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Probes location information.
    /// </summary>
    public class LocationProbe : GpsProbe
    {
        protected override int Id
        {
            get { return 3; }
        }

        protected override string DefaultDisplayName
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

            return new LocationDatum(Protocol.UserId, Id, reading.Timestamp.UtcTicks, reading.Accuracy, reading.Latitude, reading.Longitude);
        }
    }
}

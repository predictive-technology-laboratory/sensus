using System;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    public abstract class AltitudeProbe : ListeningProbe
    {
        protected override int Id
        {
            get { return 1; }
        }

        protected override string DefaultDisplayName
        {
            get { return "Altitude"; }
        }
    }
}

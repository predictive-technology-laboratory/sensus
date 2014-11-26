using System;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    public abstract class AltitudeProbe : ListeningProbe
    {
        protected override string DisplayName
        {
            get { return "Altitude"; }
        }
    }
}

using System;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    [Serializable]
    public abstract class AltitudeProbe : PassiveProbe
    {
        protected override string DisplayName
        {
            get { return "Altitude"; }
        }
    }
}

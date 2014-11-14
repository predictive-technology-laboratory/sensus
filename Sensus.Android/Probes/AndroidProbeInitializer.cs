using Android.Content;
using Sensus.Probes;
using Sensus.Probes.Location;
using Xamarin.Geolocation;

namespace Sensus.Android.Probes
{
    /// <summary>
    /// Initializes protocols and their probes with platform-specific bindings.
    /// </summary>
    public class AndroidProbeInitializer : ProbeInitializer
    {
        private Context _context;

        public AndroidProbeInitializer(Context context)
        {
            _context = context;
        }

        protected override ProbeState Initialize(Probe probe)
        {
            if(base.Initialize(probe) == ProbeState.Initializing)
            {
                if (probe is GpsProbe)
                {
                    GpsProbe gpsProbe = probe as GpsProbe;
                    Geolocator locator = new Geolocator(_context);
                    if (locator.IsGeolocationEnabled)
                    {
                        gpsProbe.SetLocator(locator);
                        gpsProbe.Initialize();
                    }
                    else
                        gpsProbe.ChangeState(ProbeState.Initializing, ProbeState.Unsupported);
                }
            }

            return probe.State;
        }
    }
}

using Android.Hardware;
using SensusService.Probes.Location;
using System;

namespace Sensus.Android.Probes.Location
{
    public class AndroidAltitudeProbe : AltitudeProbe
    {
        private AndroidSensorListener _altitudeListener;

        public AndroidAltitudeProbe()
        {
            _altitudeListener = new AndroidSensorListener(SensorType.Pressure, SensorDelay.Normal, null, e =>
                {
                    // http://www.srh.noaa.gov/images/epz/wxcalc/pressureAltitude.pdf
                    double hPa = e.Values[0];
                    double stdPressure = 1013.25;
                    double altitude = (1 - Math.Pow((hPa / stdPressure), 0.190284)) * 145366.45;

                    StoreDatum(new AltitudeDatum(this, DateTimeOffset.UtcNow, -1, altitude));
                });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _altitudeListener.Initialize();
        }

        protected override void StartListening()
        {
            _altitudeListener.Start();
        }

        protected override void StopListening()
        {
            _altitudeListener.Stop();
        }
    }
}
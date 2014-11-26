using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Sensus.Probes.Location;
using Android.Hardware;
using Sensus.Probes;

namespace Sensus.Android.Probes.Location
{
    public class AndroidCompassProbe : CompassProbe
    {
        private AndroidSensorListener _compassListener;

        public AndroidCompassProbe()
        {
            _compassListener = new AndroidSensorListener(SensorType.Orientation, SensorDelay.Normal, null, e =>
                            {
                                StoreDatum(new CompassDatum(Id, new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0)), e.Values[0]));
                            });

            Supported = _compassListener.Supported;
        }

        public override void StartListening()
        {
            _compassListener.Start();
        }

        public override void StopListening()
        {
            _compassListener.Stop();
        }
    }
}
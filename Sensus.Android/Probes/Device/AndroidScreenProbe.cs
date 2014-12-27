using Android.App;
using Android.OS;
using SensusService;
using SensusService.Probes.Device;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Device
{
    public class AndroidScreenProbe : ScreenProbe
    {
        private PowerManager _powerManager;

        public AndroidScreenProbe()
        {
            _powerManager = Application.Context.GetSystemService(global::Android.Content.Context.PowerService) as PowerManager;
        }

        public override IEnumerable<Datum> Poll()
        {
            return new Datum[] { new ScreenDatum(this, DateTimeOffset.UtcNow, _powerManager.IsScreenOn) };
        }
    }
}
using Android.App;
using Android.Content;
using Android.OS;
using SensusService.Probes.Device;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Device
{
    public class AndroidBatteryProbe : BatteryProbe
    {
        public override IEnumerable<SensusService.Datum> Poll()
        {
            Intent lastIntent = Application.Context.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));
            if (lastIntent == null)
                return new BatteryDatum[] { };
            else
                return new BatteryDatum[] { new BatteryDatum(this, DateTimeOffset.UtcNow, lastIntent.GetIntExtra(BatteryManager.ExtraLevel, -1)) };
        }
    }
}
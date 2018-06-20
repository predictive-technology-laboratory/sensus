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
using Sensus.Probes.Device;

namespace Sensus.Android.Probes.Device
{

    public class AndroidPowerConnectionChange : PowerConnectionChange
    {
        public AndroidPowerConnectionChange()
        {
            AndroidPowerConnectionChangeBroadcastReceiver.POWER_CONNECTION_CHANGE += (sender, connected) =>
            {
                POWER_CONNECTION_CHANGE?.Invoke(sender, connected);
            };
        }

    }
}
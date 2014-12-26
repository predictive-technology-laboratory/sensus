using Android.App;
using Android.Bluetooth;
using Android.Content;
using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { BluetoothDevice.ActionFound }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidBluetoothBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<BluetoothDeviceProximityDatum> DeviceFound;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (DeviceFound != null && intent != null && intent.Action == BluetoothDevice.ActionFound)
            {
                BluetoothDevice device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                DeviceFound(this, new BluetoothDeviceProximityDatum(null, DateTimeOffset.UtcNow, device.Name, device.Address));
            }
        }
    }
}
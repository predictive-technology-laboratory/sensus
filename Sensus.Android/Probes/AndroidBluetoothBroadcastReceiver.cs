using Android.App;
using Android.Bluetooth;
using Android.Content;
using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { BluetoothDevice.ActionFound }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidBluetoothBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<BluetoothDeviceProximityDatum> DeviceFound;
        private static object _staticLockObject = new object();

        public static void Stop()
        {
            lock (_staticLockObject)
                DeviceFound = null;
        }

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            lock (this)
                if (intent != null && intent.Action == BluetoothDevice.ActionFound && DeviceFound != null)
                {
                    BluetoothDevice device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                    DeviceFound(this, new BluetoothDeviceProximityDatum(null, DateTimeOffset.UtcNow, device.Name, device.Address));
                }
        }
    }
}
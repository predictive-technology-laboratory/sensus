using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private EventHandler<BluetoothDeviceProximityDatum> _deviceFoundCallback;

        public AndroidBluetoothDeviceProximityProbe()
        {
            _deviceFoundCallback = (sender, bluetoothDeviceProximityDatum) =>
                {
                    // broadcast receiver doesn't set probe
                    bluetoothDeviceProximityDatum.ProbeType = GetType().FullName;
                    StoreDatum(bluetoothDeviceProximityDatum);
                };
        }

        public override void StartListening()
        {
            AndroidBluetoothBroadcastReceiver.DeviceFound += _deviceFoundCallback;
        }

        public override void StopListening()
        {
            AndroidBluetoothBroadcastReceiver.DeviceFound -= _deviceFoundCallback;
        }
    }
}

using SensusService;
using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private EventHandler<BluetoothDeviceProximityDatum> _deviceFoundCallback;

        protected override bool Initialize()
        {
            try
            {
                _deviceFoundCallback = (sender, bluetoothDeviceProximityDatum) =>
                {
                    bluetoothDeviceProximityDatum.ProbeType = GetType().FullName;
                    StoreDatum(bluetoothDeviceProximityDatum);
                };

                return base.Initialize();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to initialize " + GetType().FullName + ":  " + ex.Message, LoggingLevel.Normal);
                return false;
            }
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
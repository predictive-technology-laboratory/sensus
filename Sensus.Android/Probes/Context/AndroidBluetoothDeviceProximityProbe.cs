
using SensusService.Probes.Context;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        public override void StartListening()
        {
            AndroidBluetoothBroadcastReceiver.DeviceFound += (o, d) =>
                {
                    d.ProbeType = GetType().FullName;
                    StoreDatum(d);
                };
        }

        public override void StopListening()
        {
            AndroidBluetoothBroadcastReceiver.Stop();
        }
    }
}
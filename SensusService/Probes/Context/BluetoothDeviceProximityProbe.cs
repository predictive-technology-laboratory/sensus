
namespace SensusService.Probes.Context
{
    public abstract class BluetoothDeviceProximityProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Bluetooth Devices"; }
        }
    }
}

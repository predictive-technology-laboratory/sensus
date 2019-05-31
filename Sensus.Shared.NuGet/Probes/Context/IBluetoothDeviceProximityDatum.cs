namespace Sensus.Probes.Context
{
    public interface IBluetoothDeviceProximityDatum : IDatum
    {
        string EncounteredDeviceId { get; set; }
		int RSSI { get; set; }
    }
}
namespace Sensus.Probes.Context
{
    public interface IBluetoothDeviceProximityDatum : IDatum
    {
        string EncounteredDeviceId { get; set; }
		int Rssi { get; set; }
    }
}
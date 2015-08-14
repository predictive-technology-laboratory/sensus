namespace Empatica.iOS
{
    public enum BLEStatus : uint
    {
        NotAvailable,
        Ready,
        Scanning
    }

    public enum DeviceStatus : uint
    {
        Disconnected,
        Connecting,
        Connected,
        FailedToConnect,
        Disconnecting
    }
}
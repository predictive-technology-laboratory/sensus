using System;

namespace Sensus
{
    public interface IDatum
    {
        string Id { get; set; }
        string DeviceId { get; set; }
        DateTimeOffset Timestamp { get; set; }
        string ProtocolId { get; set; }
        string ParticipantId { get; set; }
        string DeviceManufacturer { get; set; }
        string DeviceModel { get; set; }
        string OperatingSystem { get; set; }
    }
}
namespace Sensus.Probes.Communication
{
    public interface ISmsDatum : IDatum
    {
        string FromNumber { get; set; }
        string ToNumber { get; set; }
        string Message { get; set; }
        bool ParticipantIsSender { get; set; }
    }
}
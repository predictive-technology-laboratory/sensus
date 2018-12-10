namespace Sensus.Probes.Communication
{
    public interface ITelephonyDatum : IDatum
    {
        double? CallDurationSeconds { get; set; }
        TelephonyState State { get; set; }
        string PhoneNumber { get; set; }
    }
}
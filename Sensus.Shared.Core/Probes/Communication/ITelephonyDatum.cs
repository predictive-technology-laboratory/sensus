namespace Sensus.Probes.Communication
{
	public interface ITelephonyDatum : IDatum
	{
		TelephonyState State { get; set; }
	}
}
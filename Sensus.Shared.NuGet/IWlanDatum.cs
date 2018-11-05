namespace Sensus.Probes.Network
{
    public interface IWlanDatum : IDatum
    {
        string AccessPointBSSID { get; set; }
    }
}
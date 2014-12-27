
namespace SensusService.Probes.Network
{
    /// <summary>
    /// Probes information about WLAN access points.
    /// </summary>
    public abstract class WlanProbe : ListeningProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Wireless LAN"; }
        }
    }
}

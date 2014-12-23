
namespace SensusService.Probes.Communication
{
    /// <summary>
    /// Probes information about telephony activities (e.g., calls).
    /// </summary>
    public abstract class TelephonyProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Telephony"; }
        }
    }
}


namespace SensusService.Probes.Communication
{
    /// <summary>
    /// Probes information about incoming and outgoing phone calls.
    /// </summary>
    public abstract class TelephonyProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Phone Calls"; }
        }
    }
}

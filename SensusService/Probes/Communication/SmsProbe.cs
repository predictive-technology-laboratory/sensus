
namespace SensusService.Probes.Communication
{
    /// <summary>
    /// Probes information about SMS messages sent and received.
    /// </summary>
    public abstract class SmsProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Text Messages"; }
        }
    }
}

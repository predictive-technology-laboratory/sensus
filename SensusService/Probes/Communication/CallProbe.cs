
namespace SensusService.Probes.Communication
{
    /// <summary>
    /// Probes information about incoming and outgoing calls.
    /// </summary>
    public abstract class CallProbe : ListeningProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Calls"; }
        }
    }
}

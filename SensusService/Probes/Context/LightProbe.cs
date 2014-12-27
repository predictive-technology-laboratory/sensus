
namespace SensusService.Probes.Context
{
    public abstract class LightProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Light"; }
        }
    }
}

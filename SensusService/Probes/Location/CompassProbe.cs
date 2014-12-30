
namespace SensusService.Probes.Location
{
    public abstract class CompassProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Compass"; }
        }
    }
}

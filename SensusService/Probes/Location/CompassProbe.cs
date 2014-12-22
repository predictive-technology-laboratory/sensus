
namespace SensusService.Probes.Location
{
    public abstract class CompassProbe : ListeningProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Compass"; }
        }
    }
}

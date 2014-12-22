
namespace SensusService.Probes.Location
{
    public abstract class AltitudeProbe : ListeningProbe
    {
        protected override string DefaultDisplayName
        {
            get { return "Altitude"; }
        }
    }
}

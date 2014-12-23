
namespace SensusService.Probes.Location
{
    public abstract class AltitudeProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Altitude"; }
        }
    }
}


namespace SensusService.Probes.Movement
{
    /// <summary>
    /// Probes information about physical acceleration in x, y, and z directions.
    /// </summary>
    public abstract class AccelerometerProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Accelerometer"; }
        }
    }
}

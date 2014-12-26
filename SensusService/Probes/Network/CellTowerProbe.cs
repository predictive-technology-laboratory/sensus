
namespace SensusService.Probes.Network
{
    /// <summary>
    /// Probes information about the cell tower to which the device is bound.
    /// </summary>
    public abstract class CellTowerProbe : ListeningProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Cell Tower"; }
        }
    }
}

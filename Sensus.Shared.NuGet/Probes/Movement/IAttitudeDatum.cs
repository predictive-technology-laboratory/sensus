
namespace Sensus.Probes.Movement
{
    public interface IAttitudeDatum: IDatum
    {
        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }
        double W { get; set; }
    }
}

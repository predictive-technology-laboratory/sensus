namespace Sensus.Probes.Movement
{
    public interface ISpeedDatum : IDatum
    {
        double KPH { get; set; }
    }
}
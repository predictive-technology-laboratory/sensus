namespace Sensus.Probes.User.Scripts
{
    public interface IScriptRunner
    {
        IScript IScript { get; }
        string Name { get; set; }
        double? MaxAgeMinutes { get; set; }
        bool WindowExpiration { get; set; }
        string TriggerWindowsString { get; set; }
        int NonDowTriggerIntervalDays { get; set; }
    }
}
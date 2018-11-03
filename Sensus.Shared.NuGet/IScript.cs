using System;

namespace Sensus.Probes.User.Scripts
{
    public interface IScript
    {
        string Id { get; set; }
        DateTimeOffset? ScheduledRunTime { get; set; }
        DateTimeOffset? RunTime { get; set; }
        DateTime? ExpirationDate { get; set; }
        DateTime Birthdate { get; }
        DateTime DisplayDateTime { get; }
    }
}
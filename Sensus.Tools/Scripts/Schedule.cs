using System;

namespace Sensus.Tools.Scripts
{
    public class Schedule
    {
        public DateTime RunTime => DateTime.Now + TimeUntil;
        public TimeSpan TimeUntil { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
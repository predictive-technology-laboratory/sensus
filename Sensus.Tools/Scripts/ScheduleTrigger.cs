using System;
using Sensus.Tools.Extensions;

namespace Sensus.Tools.Scripts
{
    public class ScheduleTrigger : IComparable<ScheduleTrigger>
    {
        #region Static Methods
        public static ScheduleTrigger Parse(string window)
        {
            var startEnd = window.Trim().Split('-');

            if (startEnd.Length == 1)
            {
                return new ScheduleTrigger
                {
                    //for some reason DateTime.Parse seems to be more forgiving
                    Start = DateTime.Parse(startEnd[0].Trim()).TimeOfDay,
                    End   = DateTime.Parse(startEnd[0].Trim()).TimeOfDay,
                };
            }

            if (startEnd.Length == 2)
            {
                var result = new ScheduleTrigger
                {
                    //for some reason DateTime.Parse seems to be more forgiving
                    Start = DateTime.Parse(startEnd[0].Trim()).TimeOfDay,
                    End   = DateTime.Parse(startEnd[1].Trim()).TimeOfDay,
                };

                if (result.Start > result.End)
                {
                    throw new Exception($"Improper trigger byTime ({window})");
                }

                return result;
            }

            throw new Exception($"Improper trigger byTime ({window})");
        }
        #endregion

        #region Properties
        public TimeSpan Start { get; private set; }
        public TimeSpan End { get; private set; }
        public TimeSpan Window => End - Start;
        #endregion

        #region Public Methods
        /// <remarks>
        /// If we are currently in the previous window we skip it. This may not be perfect but it makes everything else infinitely simpler to do.
        /// </remarks>
        public Schedule NextSchedule(DateTime from, DateTime after, bool windowExpiration, TimeSpan? maxAge)
        {
            var timeUntilRng = TimeBetween(from, after) + TimeTillStart(after.TimeOfDay) + RandomWindowTime();
            var timeUntilEnd = TimeBetween(from, after) + TimeTillEnd(after.TimeOfDay);

            var winExpiration = windowExpiration ? from.Add(timeUntilEnd) : DateTime.MaxValue;
            var ageExpiration = maxAge != null   ? from.Add(timeUntilRng).Add(maxAge.Value) : DateTime.MaxValue;

            return new Schedule
            {
                TimeUntil  = timeUntilRng,
                ExpirationDate = winExpiration.Min(ageExpiration)
            };
        }

        public override string ToString()
        {
            //String interpolation doesn't seem to work here for some reason. E.g., $"{Start:hh:mm}"
            return Start == End ? Start.ToString("hh\\:mm") : Start.ToString("hh\\:mm") + "-" + End.ToString("hh\\:mm");
        }

        public int CompareTo(ScheduleTrigger comparee)
        {
            return Start.CompareTo(comparee.Start);
        }
        #endregion

        #region Private Methods        
        private TimeSpan TimeBetween(DateTime start, DateTime end)
        {
            return end - start;
        }

        private TimeSpan TimeTillStart(TimeSpan time)
        {
            return TimeTill(time, Start);
        }

        private TimeSpan TimeTillEnd(TimeSpan time)
        {
            return TimeTill(time, Start) + TimeTill(Start, End);
        }

        private TimeSpan TimeTill(TimeSpan start, TimeSpan end)
        {
            var timeTillStart = (end - start).Ticks;

            if (timeTillStart <= 0)
            {
                timeTillStart += TimeSpan.TicksPerDay;
            }

            return TimeSpan.FromTicks(timeTillStart);
        }

        private TimeSpan RandomWindowTime()
        {
            var zeroToOne = new Random((int)DateTime.Now.Ticks).NextDouble();

            return TimeSpan.FromTicks((long)(Window.Ticks * zeroToOne));
        }
        #endregion
    }
}
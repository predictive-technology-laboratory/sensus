// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Sensus.Tools.Extensions;

namespace Sensus.Tools.Scripts
{
    public class TriggerWindow : IComparable<TriggerWindow>
    {
        #region Static Methods
        public static TriggerWindow Parse(string window)
        {
            var startEnd = window.Trim().Split('-');

            if (startEnd.Length == 1)
            {
                return new TriggerWindow
                {
                    //for some reason DateTime.Parse seems to be more forgiving
                    Start = DateTime.Parse(startEnd[0].Trim()).TimeOfDay,
                    End = DateTime.Parse(startEnd[0].Trim()).TimeOfDay
                };
            }

            if (startEnd.Length == 2)
            {
                var result = new TriggerWindow
                {
                    //for some reason DateTime.Parse seems to be more forgiving
                    Start = DateTime.Parse(startEnd[0].Trim()).TimeOfDay,
                    End = DateTime.Parse(startEnd[1].Trim()).TimeOfDay
                };

                if (result.Start > result.End)
                {
                    throw new Exception($"Improper trigger window ({window})");
                }

                return result;
            }

            throw new Exception($"Improper trigger window ({window})");
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
        public ScriptTriggerTime GetNextTriggerTime(DateTime from, DateTime after, bool windowExpiration, TimeSpan? maxAge)
        {
            var timeTillTrigger = TimeBetween(from, after) + TimeTillFutureStart(after.TimeOfDay) + RandomWindowTime();
            var timeTillTriggerWindowEnd = TimeBetween(from, after) + TimeTillFutureEnd(after.TimeOfDay);

            var windowExpirationDateTime = Start == End ? DateTime.MaxValue : windowExpiration ? from.Add(timeTillTriggerWindowEnd) : DateTime.MaxValue;
            var ageExpirationDateTime = maxAge != null ? from.Add(timeTillTrigger).Add(maxAge.Value) : DateTime.MaxValue;

            return new ScriptTriggerTime
            {
                TimeTill = timeTillTrigger,
                Expiration = windowExpirationDateTime.Min(ageExpirationDateTime)
            };
        }

        public override string ToString()
        {
            // String interpolation doesn't seem to work here for some reason. E.g., $"{Start:hh:mm}"
            return Start == End ? Start.ToString("hh\\:mm") : Start.ToString("hh\\:mm") + "-" + End.ToString("hh\\:mm");
        }

        public int CompareTo(TriggerWindow comparee)
        {
            return Start.CompareTo(comparee.Start);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Calculates time elapsed from a start to end.
        /// </summary>
        /// <returns>Elapsed.</returns>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        private TimeSpan TimeBetween(DateTime start, DateTime end)
        {
            return end - start;
        }

        /// <summary>
        /// Calculates the time from a reference to the next future occurrence of this window's start.
        /// </summary>
        /// <returns>The till future start.</returns>
        /// <param name="reference">Reference.</param>
        private TimeSpan TimeTillFutureStart(TimeSpan reference)
        {
            return TimeTillFutureTarget(reference, Start);
        }

        /// <summary>
        /// Calculates time from a reference until the next future occurrence of this window's end.
        /// </summary>
        /// <returns>The time till end.</returns>
        /// <param name="reference">Reference.</param>
        private TimeSpan TimeTillFutureEnd(TimeSpan reference)
        {
            return TimeTillFutureTarget(reference, Start) + (End - Start);
        }

        /// <summary>
        /// Calculates the time from a reference to the next future occurrence of a target time.
        /// </summary>
        /// <returns>Time till future target.</returns>
        /// <param name="reference">Reference.</param>
        /// <param name="target">Target.</param>
        private TimeSpan TimeTillFutureTarget(TimeSpan reference, TimeSpan target)
        {
            var ticksTillUpcomingTarget = (target - reference).Ticks;

            if (ticksTillUpcomingTarget <= 0)
            {
                ticksTillUpcomingTarget += TimeSpan.TicksPerDay;
            }

            return TimeSpan.FromTicks(ticksTillUpcomingTarget);
        }

        /// <summary>
        /// Calculates a random time into this window.
        /// </summary>
        /// <returns>The random time.</returns>
        private TimeSpan RandomWindowTime()
        {
            var zeroToOne = new Random((int)DateTime.Now.Ticks).NextDouble();

            return TimeSpan.FromTicks((long)(Window.Ticks * zeroToOne));
        }
        #endregion
    }
}
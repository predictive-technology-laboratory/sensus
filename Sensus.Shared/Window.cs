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

namespace Sensus
{
    public class Window : IComparable<Window>
    {
        #region Properties
        public TimeSpan Start { get; private set; }
        public TimeSpan End { get; private set; }
        public TimeSpan Duration => End - Start;
        #endregion

        public Window(string windowString)
        {
            string[] startEnd = windowString.Trim().Split('-');

            Start = DateTime.Parse(startEnd[0].Trim()).TimeOfDay;  // for some reason DateTime.Parse seems to be more forgiving

            if (startEnd.Length == 1)
            {
                End = Start;
            }
            else if (startEnd.Length == 2)
            {
                End = DateTime.Parse(startEnd[1].Trim()).TimeOfDay;  // for some reason DateTime.Parse seems to be more forgiving

                if (Start > End)
                {
                    throw new Exception($"Improper trigger window ({windowString})");
                }
            }
            else
            {
                throw new Exception($"Improper trigger window ({windowString})");
            }
        }

        #region Public Methods
        public override string ToString()
        {
            // String interpolation doesn't seem to work here for some reason. E.g., $"{Start:hh:mm}"
            return Start == End ? Start.ToString("hh\\:mm") : Start.ToString("hh\\:mm") + "-" + End.ToString("hh\\:mm");
        }

        public int CompareTo(Window comparee)
        {
            return Start.CompareTo(comparee.Start);
        }

        public bool Encompasses(TimeSpan time)
        {
            return time >= Start && time <= End;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Calculates time elapsed from a start to end.
        /// </summary>
        /// <returns>Elapsed.</returns>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        protected TimeSpan TimeBetween(DateTime start, DateTime end)
        {
            return end - start;
        }

        /// <summary>
        /// Calculates the time from a reference to the next future occurrence of this window's start.
        /// </summary>
        /// <returns>The till future start.</returns>
        /// <param name="reference">Reference.</param>
        protected TimeSpan TimeTillFutureStart(TimeSpan reference)
        {
            return TimeTillFutureTarget(reference, Start);
        }

        /// <summary>
        /// Calculates time from a reference until the next future occurrence of this window's end.
        /// </summary>
        /// <returns>The time till end.</returns>
        /// <param name="reference">Reference.</param>
        protected TimeSpan TimeTillFutureEnd(TimeSpan reference)
        {
            return TimeTillFutureTarget(reference, Start) + (End - Start);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Calculates the time from a reference to the next future occurrence of a target time.
        /// </summary>
        /// <returns>Time till future target.</returns>
        /// <param name="reference">Reference.</param>
        /// <param name="target">Target.</param>
        private TimeSpan TimeTillFutureTarget(TimeSpan reference, TimeSpan target)
        {
            var ticksTillUpcomingTarget = (target - reference).Ticks;

            // if target comes before the reference time (e.g., 10am target and 1pm reference), then skip ahead to the
            // next day (e.g., producing 1pm today as reference and 10am tomorrow as target)
            if (ticksTillUpcomingTarget <= 0)
            {
                ticksTillUpcomingTarget += TimeSpan.TicksPerDay;
            }

            return TimeSpan.FromTicks(ticksTillUpcomingTarget);
        }
        #endregion
    }
}

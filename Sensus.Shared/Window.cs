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
using System.Collections.Generic;
using System.Linq;
using Sensus.Exceptions;

namespace Sensus
{
    public class Window : IComparable<Window>
    {
        #region Properties
        public DayOfWeek? DayOfTheWeek { get; private set; }
        public TimeSpan Start { get; private set; }
        public TimeSpan End { get; private set; }
        public TimeSpan Duration => End - Start;
        #endregion

        public Window(string windowString)
        {
            string[] windowStringParts = windowString.Trim().Split('-');  // format is DD-HH:MM-HH:MM, where the first and last components are optional

            // check whether the first element is a DOW abbreviation
            string firstElement = windowStringParts[0].Trim();
            List<string> dayOfTheWeekAbbreviations = new List<string>(new string[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" });
            if (dayOfTheWeekAbbreviations.Contains(firstElement))
            {
                // get enumeration value for abbreviation
                foreach (string dayOfTheWeek in Enum.GetNames(typeof(DayOfWeek)))
                {
                    if (dayOfTheWeek.StartsWith(firstElement))
                    {
                        DayOfTheWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfTheWeek);
                        break;
                    }
                }

                // the string started with a known abbreviation, so we should have obtained the enumeration value.
                if (DayOfTheWeek == null)
                {
                    throw SensusException.Report("Unable to obtain DayOfWeek for abbreviation:  " + firstElement);
                }

                // trim DOW abbreviation from start of array.
                windowStringParts = windowStringParts.Skip(1).ToArray();
            }

            Start = DateTime.Parse(windowStringParts[0].Trim()).TimeOfDay;  // for some reason DateTime.Parse seems to be more forgiving

            if (windowStringParts.Length == 1)
            {
                End = Start;
            }
            else if (windowStringParts.Length == 2)
            {
                End = DateTime.Parse(windowStringParts[1].Trim()).TimeOfDay;  // for some reason DateTime.Parse seems to be more forgiving

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
            string dowAbbreviation = "";
            if (DayOfTheWeek.HasValue)
            {
                dowAbbreviation = DayOfTheWeek.Value.ToString().Substring(0, 2).ToLower();
                dowAbbreviation = dowAbbreviation[0].ToString().ToUpper() + dowAbbreviation[1];
            }

            // String interpolation doesn't seem to work here for some reason. E.g., $"{Start:hh:mm}"
            return dowAbbreviation + (Start == End ? Start.ToString("hh\\:mm") : Start.ToString("hh\\:mm") + "-" + End.ToString("hh\\:mm"));
        }

        public int CompareTo(Window comparee)
        {
            int dowCmp = 0;

            // only compare DOW if both objects have one
            if (DayOfTheWeek.HasValue && comparee.DayOfTheWeek.HasValue)
            {
                // comparison is 0 (Sunday) - 6 (Saturday)
                dowCmp = DayOfTheWeek.Value.CompareTo(comparee.DayOfTheWeek.Value);
            }

            // compare start times if the DOWs are the same (or not comparable)
            if (dowCmp == 0)
            {
                dowCmp = Start.CompareTo(comparee.Start);
            }

            return dowCmp;
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
            return TimeTillFutureStart(reference) + (End - Start);
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

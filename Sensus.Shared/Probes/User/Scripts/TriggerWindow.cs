//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Sensus.Extensions;

namespace Sensus.Probes.User.Scripts
{
    public class TriggerWindow : Window
    {
        public TriggerWindow(string windowString)
            : base(windowString)
        {
        }

        #region Public Methods
        /// <summary>
        /// Gets the next trigger time.
        /// </summary>
        /// <returns>The next trigger time.</returns>
        /// <param name="after">The time after which the trigger time should occur.</param>
        /// <param name="windowExpiration">Whether or not to expire at the current window's end.</param>
        /// <param name="maxAge">Maximum age of the triggered script.</param>
        public ScriptTriggerTime GetNextTriggerTime(DateTime after, bool windowExpiration, TimeSpan? maxAge)
        {
            double zeroToOne = new Random((int)DateTime.Now.Ticks).NextDouble();
            TimeSpan randomIntervalIntoWindow = TimeSpan.FromTicks((long)(Duration.Ticks * zeroToOne));
            DateTime triggerDateTime = after + TimeTillFutureStart(after.TimeOfDay) + randomIntervalIntoWindow; 
            
            // it only makes sense to do window expiration when the window timespan is not zero. if we did window expiration with
            // a zero-length window the script would expire immediately.
            DateTime? windowExpirationDateTime = default(DateTime?);
            if (windowExpiration && Start != End)
            {
                windowExpirationDateTime = after + TimeTillFutureEnd(after.TimeOfDay);
            }

            DateTime? ageExpirationDateTime = default(DateTime?);
            if (maxAge != null)
            {
                ageExpirationDateTime = triggerDateTime.Add(maxAge.Value);
            }

            return new ScriptTriggerTime(triggerDateTime, windowExpirationDateTime.Min(ageExpirationDateTime), ToString());
        }

        public string GetReadableDescription(int nonDowTriggerIntervalDays)
        {
            string description = "";

            if (DayOfTheWeek.HasValue)
            {
                description = "on " + DayOfTheWeek.Value + "s ";
            }
            else
            {
                if (nonDowTriggerIntervalDays == 1)
                {
                    description = "each day ";
                }
                else if (nonDowTriggerIntervalDays == 2)
                {
                    description = "every other day ";
                }
                else
                {
                    description = "every " + nonDowTriggerIntervalDays + " days ";
                }
            }

            if (Start == End)
            {
                description += "at " + Start.ToString("hh\\:mm");
            }
            else
            {
                description += "between " + Start.ToString("hh\\:mm") + " and " + End.ToString("hh\\:mm");
            }

            return description;
        }
        #endregion
    }
}

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
        /// <param name="reference">The reference time, from which the next time should be computed.</param>
        /// <param name="after">The time after which the trigger time should occur.</param>
        /// <param name="windowExpiration">Whether or not to expire at the current window's end.</param>
        /// <param name="maxAge">Maximum age of the triggered script.</param>
        public ScriptTriggerTime GetNextTriggerTime(DateTime reference, DateTime after, bool windowExpiration, TimeSpan? maxAge)
        {
            double zeroToOne = new Random((int)DateTime.Now.Ticks).NextDouble();
            TimeSpan randomIntervalIntoWindow = TimeSpan.FromTicks((long)(Duration.Ticks * zeroToOne));
            TimeSpan timeTillTrigger = TimeBetween(reference, after) + TimeTillFutureStart(after.TimeOfDay) + randomIntervalIntoWindow;
            DateTime triggerDateTime = reference.Add(timeTillTrigger);
            
            // it only makes sense to do window expiration when the window timespan is not zero. if we did window expiration with
            // a zero-length window the script would expire immediately.
            DateTime? windowExpirationDateTime = default(DateTime?);
            if (windowExpiration && Start != End)
            {
                TimeSpan timeTillTriggerWindowEnd = TimeBetween(reference, after) + TimeTillFutureEnd(after.TimeOfDay);
                windowExpirationDateTime = reference.Add(timeTillTriggerWindowEnd);
            }

            DateTime? ageExpirationDateTime = default(DateTime?);
            if (maxAge != null)
            {
                ageExpirationDateTime = triggerDateTime.Add(maxAge.Value);
            }

            return new ScriptTriggerTime(reference, triggerDateTime, windowExpirationDateTime.Min(ageExpirationDateTime), ToString());
        }
        #endregion
    }
}
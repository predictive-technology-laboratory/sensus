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
using System.Linq;
using System.Collections.Generic;

namespace Sensus.Probes.User.Scripts
{
    public class ScheduleTrigger
    {
        #region Fields
        private readonly List<TriggerWindow> _windows;
        private int _intervalDays;
        #endregion

        #region Properties
        public int WindowCount => _windows.Count;

        public string WindowsString
        {
            get
            {
                lock (_windows)
                {
                    return string.Join(", ", _windows);
                }
            }
            set
            {
                if (value == WindowsString)
                    return;

                lock (_windows)
                {
                    _windows.Clear();

                    try
                    {
                        _windows.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(windowString => new TriggerWindow(windowString)));
                    }
                    catch
                    {
                        // ignore improperly formatted trigger windows
                    }

                    _windows.Sort();
                }
            }
        }

        public int IntervalDays
        {
            get
            {
                return _intervalDays;
            }
            set
            {
                _intervalDays = value;
            }
        }

        public bool WindowExpiration { get; set; }
        #endregion

        #region Constructor
        public ScheduleTrigger()
        {
            _windows = new List<TriggerWindow>();
            _intervalDays = 1;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets trigger times relative to a given time after some other time.
        /// </summary>
        /// <returns>Trigger times.</returns>
        /// <param name="reference">The reference time, from which the next time should be computed.</param>
        /// <param name="after">The time after which the trigger time should occur.</param>
        /// <param name="maxAge">Maximum age of the trigger.</param>
        public List<ScriptTriggerTime> GetTriggerTimes(DateTime reference, DateTime after, TimeSpan? maxAge = null)
        {
            lock (_windows)
            {
                // we used to use a yield-return approach for returning the trigger times; however, there's an issue:  the reference time does not 
                // change, and if there are significant latencies involved in scheduling the returned trigger time then the notification time will
                // not accurately reflect the requested trigger reference. so, the better approach is to gather all triggers immediately to minimize
                // the effect of such latencies.
                List<ScriptTriggerTime> triggerTimes = new List<ScriptTriggerTime>();

                // return 7 triggers for each window
                for (int triggerNum = 0; triggerNum < 7; ++triggerNum)
                {
                    foreach (TriggerWindow window in _windows)
                    {
                        DateTime afterCurr;

                        // if the window has a day-of-week specified, ignore the interval days field and go week by week instead
                        if (window.DayOfTheWeek.HasValue)
                        {
                            // how many days until the specified day of week?
                            int daysUntilDOW = 0;
                            if (after.DayOfWeek < window.DayOfTheWeek.Value)
                            {
                                daysUntilDOW = window.DayOfTheWeek.Value - after.DayOfWeek;
                            }
                            else if (after.DayOfWeek > window.DayOfTheWeek.Value)
                            {
                                // number of days until saturday + number of days from saturday to window's DOW
                                daysUntilDOW = ((int)DayOfWeek.Saturday - (int)after.DayOfWeek) + (int)window.DayOfTheWeek.Value + 1;
                            }

                            afterCurr = after.AddDays(triggerNum * 7 + daysUntilDOW);
                        }
                        else
                        {
                            afterCurr = after.AddDays(triggerNum * _intervalDays);
                        }

                        ScriptTriggerTime triggerTime = window.GetNextTriggerTime(reference, afterCurr, WindowExpiration, maxAge);

                        triggerTimes.Add(triggerTime);
                    }
                }

                // it is important that these are ordered otherwise we might skip windows since we use the _maxScheduledDate to determine which schedule comes next.
                triggerTimes.OrderBy(t => t.Trigger);

                return triggerTimes;
            }
        }
        #endregion
    }
}
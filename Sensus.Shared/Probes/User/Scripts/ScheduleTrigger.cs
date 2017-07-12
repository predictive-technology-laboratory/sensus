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
        private int _nonDowTriggerIntervalDays;
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

        public int NonDowTriggerIntervalDays
        {
            get
            {
                return _nonDowTriggerIntervalDays;
            }
            set
            {
                _nonDowTriggerIntervalDays = value;
            }
        }

        public bool WindowExpiration { get; set; }
        #endregion

        #region Constructor
        public ScheduleTrigger()
        {
            _windows = new List<TriggerWindow>();
            _nonDowTriggerIntervalDays = 1;
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
                        DateTime currTriggerAfter;

                        // if the window has a day-of-week specified, ignore the interval days field and go week-by-week instead
                        if (window.DayOfTheWeek.HasValue)
                        {
                            // how many days from the after DOW to the window's DOW?
                            int daysUntilWindowDOW = 0;

                            // if the after DOW (e.g., Tuesday) precedes the window's DOW (e.g., Thursday), the answer is simple (e.g., 2)
                            if (after.DayOfWeek < window.DayOfTheWeek.Value)
                            {
                                daysUntilWindowDOW = window.DayOfTheWeek.Value - after.DayOfWeek;
                            }
                            // if the after DOW (e.g., Wednesday) is after the window's DOW (e.g., Monday), we need to wrap around (e.g., 5)
                            else if (after.DayOfWeek > window.DayOfTheWeek.Value)
                            {
                                // number of days until saturday + number of days from saturday to window's DOW
                                daysUntilWindowDOW = (DayOfWeek.Saturday - after.DayOfWeek) + (int)window.DayOfTheWeek.Value + 1;
                            }

                            // each DOW-based window is separated by a week
                            currTriggerAfter = after.AddDays(triggerNum * 7 + daysUntilWindowDOW);

                            // ensure that the trigger time is not shifted to the next day by removing the time component (i.e., setting is to 12:00am). this
                            // ensures that any window time (e.g., 1am) will be feasible.
                            currTriggerAfter = currTriggerAfter.Date;
                        }
                        else
                        {
                            // the window is interval-based, so skip ahead the current number of days
                            currTriggerAfter = after.AddDays(triggerNum * _nonDowTriggerIntervalDays);
                        }

                        ScriptTriggerTime triggerTime = window.GetNextTriggerTime(reference, currTriggerAfter, WindowExpiration, maxAge);

                        triggerTimes.Add(triggerTime);
                    }
                }

                // it is important that these are ordered otherwise we might skip windows since we use the _maxScheduledDate to determine which schedule comes next.
                triggerTimes.Sort((x, y) => x.Trigger.CompareTo(y.Trigger));

                return triggerTimes;
            }
        }
        #endregion
    }
}
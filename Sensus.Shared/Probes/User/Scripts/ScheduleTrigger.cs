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
using Newtonsoft.Json;

namespace Sensus.Probes.User.Scripts
{
    public class ScheduleTrigger
    {
        private readonly List<TriggerWindow> _windows;
        private int _nonDowTriggerIntervalDays;

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
                {
                    return;
                }

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

        [JsonIgnore]
        public string ReadableDescription
        {
            get
            {
                if (_windows.Count == 0)
                {
                    return "";
                }
                else if (_windows.Count == 1)
                {
                    return _windows[0].GetReadableDescription(_nonDowTriggerIntervalDays);
                }
                else if (_windows.Count == 2)
                {
                    return _windows[0].GetReadableDescription(_nonDowTriggerIntervalDays) + " and " + _windows[1].GetReadableDescription(_nonDowTriggerIntervalDays);
                }
                else
                {
                    return string.Concat(_windows.Take(_windows.Count - 1).Select(window => window.GetReadableDescription(_nonDowTriggerIntervalDays) + ", ")) + " and " + _windows.Last().GetReadableDescription(_nonDowTriggerIntervalDays);
                }
            }
        }

        public ScheduleTrigger()
        {
            _windows = new List<TriggerWindow>();
            _nonDowTriggerIntervalDays = 1;
        }

        /// <summary>
        /// Gets trigger times relative to a given reference, starting on a particular date and having a maximum age until
        /// expiration.
        /// </summary>
        /// <returns>Trigger times.</returns>
        /// <param name="startDate">The date on which the scheduled triggers should start. Only the year, month, and day elements will be considered.</param>
        /// <param name="maxAge">Maximum age of the triggers, during which they should be valid.</param>
        public List<ScriptTriggerTime> GetTriggerTimes(DateTime startDate, TimeSpan? maxAge = null)
        {
            lock (_windows)
            {
                // we used to use a yield-return approach for returning the trigger times; however, there's an issue:  the reference time does not 
                // change, and if there are significant latencies involved in scheduling the returned trigger time then the notification time will
                // not accurately reflect the requested trigger reference. so, the better approach is to gather all triggers immediately to minimize
                // the effect of such latencies.
                List<ScriptTriggerTime> triggerTimes = new List<ScriptTriggerTime>();

                // ignore the time component of the start date. get all times on the given day.
                startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);

                // schedule enough days to ensure that all windows get at least one trigger. for DOW windows, this
                // means that we must schedule enough days to cover all days of the week (10 will suffice).  for
                // time-of-day-winows, this means that we must schedule at least the number of days specified in the
                // interval. the reason this is important is that, if the number of days that we schedule does not
                // include any trigger windows, then no surveys will be scheduled and we run the risk of losing
                // touch with the user. the health test callbacks should ensure that survey triggers continue to
                // be scheduled, so it should not be the case that we lose the user entirely. however, on ios it is
                // more likely that the user will ignore surveys without bringing the app to the foreground and giving 
                // an opportunity to schedule additional surveys.
                int numDays = Math.Max(10, _nonDowTriggerIntervalDays);

                for (int dayOffset = 0; dayOffset < numDays; ++dayOffset)
                {
                    DateTime triggerDate = startDate.AddDays(dayOffset);
                    DayOfWeek triggerDateDOW = triggerDate.DayOfWeek;

                    // schedule each window for the current date as necessary
                    foreach (TriggerWindow window in _windows)
                    {
                        bool scheduleWindowForCurrentDate = false;

                        if (window.DayOfTheWeek.HasValue)
                        {
                            if (window.DayOfTheWeek.Value == triggerDateDOW)
                            {
                                scheduleWindowForCurrentDate = true;
                            }
                        }
                        // we need a reference point for calculating the day-based interval. the minimum value will work.
                        else if ((triggerDate - DateTime.MinValue).Days % _nonDowTriggerIntervalDays == 0)
                        {
                            scheduleWindowForCurrentDate = true;
                        }

                        if (scheduleWindowForCurrentDate)
                        {
                            ScriptTriggerTime triggerTime = window.GetNextTriggerTime(triggerDate, WindowExpiration, maxAge);
                            triggerTimes.Add(triggerTime);
                        }
                    }
                }

                triggerTimes.Sort((x, y) => x.Trigger.CompareTo(y.Trigger));

                return triggerTimes;
            }
        }
    }
}
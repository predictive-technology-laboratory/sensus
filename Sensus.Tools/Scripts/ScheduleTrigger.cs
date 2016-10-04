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

namespace Sensus.Tools.Scripts
{
    public class ScheduleTrigger
    {
        #region Fields
        private readonly List<ScheduleWindow> _windows;
        #endregion

        #region Properties
        public int WindowCount => _windows.Count;
        public string Windows
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
                if (value == Windows) return;

                lock (_windows)
                {
                    _windows.Clear();

                    try
                    {
                        _windows.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(ScheduleWindow.Parse));
                    }
                    catch
                    {
                        //ignore improperly formatted trigger windows
                    }

                    _windows.Sort();
                }
            }
        }
        public TimeSpan? ExpireAge { get; set; }
        public bool ExpireWindow { get; set; }
        #endregion

        #region Constructor
        public ScheduleTrigger()
        {
            _windows = new List<ScheduleWindow>(); 
        }
        #endregion

        #region Public Methods
        public IEnumerable<Schedule> SchedulesAfter(DateTime startDate, DateTime afterDate)
        {
            var eightDays = TimeSpan.FromDays(8);
            var oneDay    = TimeSpan.FromDays(1);

            lock (_windows)
            {
                for (; afterDate - startDate < eightDays; afterDate += oneDay)
                {
                    //It is important that these are ordered otherwise we might skip windows since we use the _maxScheduledDate to determine which schedule comes next.
                    foreach (var schedule in _windows.Select(s => s.NextSchedule(startDate, afterDate, ExpireWindow, ExpireAge)).OrderBy(s => s.TimeUntil).ToArray())
                    {
                        yield return schedule;
                    }
                }
            }            
        }
        #endregion
    }
}
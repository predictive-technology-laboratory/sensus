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

namespace Sensus.Shared.Probes.User.Scripts
{
    public class ScheduleTrigger
    {
        #region Fields
        private readonly List<TriggerWindow> _windows;
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
                if (value == Windows)
                    return;

                lock (_windows)
                {
                    _windows.Clear();

                    try
                    {
                        _windows.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(TriggerWindow.Parse));
                    }
                    catch
                    {
                        // ignore improperly formatted trigger windows
                    }

                    _windows.Sort();
                }
            }
        }

        public bool WindowExpiration { get; set; }
        #endregion

        #region Constructor
        public ScheduleTrigger()
        {
            _windows = new List<TriggerWindow>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets trigger times relative to a given time after some other time.
        /// </summary>
        /// <returns>Trigger times.</returns>
        /// <param name="from">Get trigger times relative to this time.</param>
        /// <param name="after">Get trigger times after this time.</param>
        /// <param name="maxAge">Maximum age of the trigger.</param>
        public IEnumerable<ScriptTriggerTime> GetTriggerTimes(DateTime from, DateTime after, TimeSpan? maxAge = null)
        {
            var eightDays = TimeSpan.FromDays(8);
            var oneDay = TimeSpan.FromDays(1);

            lock (_windows)
            {
                // return trigger times up to 8 days beyond the from time
                for (; after - from < eightDays; after += oneDay)
                {
                    // It is important that these are ordered otherwise we might skip windows since we use the _maxScheduledDate to determine which schedule comes next.
                    foreach (var triggerTime in _windows.Select(window => window.GetNextTriggerTime(from, after, WindowExpiration, maxAge)).OrderBy(window => window.TimeTill).ToArray())
                    {
                        yield return triggerTime;
                    }
                }
            }
        }
        #endregion
    }
}
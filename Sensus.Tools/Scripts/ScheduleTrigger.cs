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
        public void DeserializeWindows(string windows)
        {
            if (windows == SerlializeWindows()) return;

            lock (_windows)
            {
                    _windows.Clear();

                    try
                    {
                        _windows.AddRange(windows.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(ScheduleWindow.Parse));
                    }
                    catch
                    {
                        //ignore improperly formatted trigger windows
                    }

                _windows.Sort();
            }
        }

        public string SerlializeWindows()
        {
            lock (_windows)
            {
                return string.Join(", ", _windows);
            }
        }

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
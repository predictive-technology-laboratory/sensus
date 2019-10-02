using EventKit;
using Foundation;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSCalendarProbe : CalendarProbe
	{
		public override int DefaultPollingSleepDurationMS => 10800000;

		protected override List<CalendarDatum> GetCalendarEventsAsync()
		{
			List<CalendarDatum> datums = new List<CalendarDatum>();
			EKEventStore store = new EKEventStore();
			EKCalendar[] calendars = store.GetCalendars(EKEntityType.Event);

			NSDate last = (NSDate)LastPollTime.AddDays(-1);
			NSDate now = (NSDate)DateTime.Now;

			NSPredicate predicate = store.PredicateForEvents(last, now, calendars);
			EKEvent[] items = store.EventsMatching(predicate);

			foreach (EKEvent item in items)
			{
				CalendarDatum datum = new CalendarDatum(item.EventIdentifier, item.Title, (DateTime)item.StartDate, (DateTime)item.EndDate, ((DateTime)item.EndDate - (DateTime)item.StartDate).TotalMilliseconds, item.Description, item.Location, item.Organizer?.Name, item.Organizer == null || item.Organizer?.IsCurrentUser == true, DateTimeOffset.UtcNow);

				datums.Add(datum);
			}

			return datums;
		}
	}
}

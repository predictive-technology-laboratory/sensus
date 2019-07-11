
using EventKit;
using Foundation;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSCalendarProbe : CalendarProbe
	{
		public override int DefaultPollingSleepDurationMS => 10800000;

		public DateTime LastPollTime { get; set; } = DateTime.Now.AddHours(-24);

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();
		}

		protected override async Task<List<CalendarDatum>> GetCalendarEventsAsync()
		{
			if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Calendar) != PermissionStatus.Granted)
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Calendar permission is not permitted on this device. Cannot start Calendar probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}

			List<CalendarDatum> datums = new List<CalendarDatum>();
			EKEventStore store = new EKEventStore();
			EKCalendar[] calendars = store.GetCalendars(EKEntityType.Event);

			NSPredicate predicate = store.PredicateForEvents((NSDate)DateTime.Now.ToLocalTime(), (NSDate)LastPollTime.ToLocalTime(), calendars);
			EKEvent[] items = store.EventsMatching(predicate);

			foreach (EKEvent item in items)
			{
				CalendarDatum datum = new CalendarDatum(DateTimeOffset.UtcNow)
				{
					Id = item.EventIdentifier,
					Title = item.Title,
					Description = item.Notes,
					EventLocation = item.Location,
					Start = item.StartDate?.ToString(),
					End = item.EndDate?.ToString(),
					Organizer = item.Organizer?.Name,
					IsOrganizer = item.Organizer == null || item.Organizer?.IsCurrentUser == true,
					Duration = ((DateTime)item.StartDate - (DateTime)item.EndDate).TotalMilliseconds
				};

				datums.Add(datum);
			}

			LastPollTime = DateTime.UtcNow;

			return datums;
		}
	}
}

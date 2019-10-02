using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using Android.Provider;
using Android.Database;
using Android.App;
using System.Linq;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidCalendarProbe : CalendarProbe
	{
		public AndroidCalendarProbe()
		{

		}

		protected override List<CalendarDatum> GetCalendarEventsAsync()
		{
			List<CalendarDatum> calendarDatums = new List<CalendarDatum>();

			string[] eventProperties = {
				CalendarContract.Events.InterfaceConsts.Id,
				CalendarContract.Events.InterfaceConsts.Title,
				CalendarContract.Events.InterfaceConsts.Dtstart,
				CalendarContract.Events.InterfaceConsts.Dtend,
				CalendarContract.Events.InterfaceConsts.Duration,
				CalendarContract.Events.InterfaceConsts.Description,
				CalendarContract.Events.InterfaceConsts.EventLocation,
				CalendarContract.Events.InterfaceConsts.Organizer,
				CalendarContract.Events.InterfaceConsts.IsOrganizer,
			};

			DateTimeOffset epoch = DateTimeOffset.FromUnixTimeMilliseconds(0);
			
			long last = ((DateTimeOffset)LastPollTime.AddDays(-1)).ToUnixTimeMilliseconds();
			long now = Java.Lang.JavaSystem.CurrentTimeMillis();

			ICursor cursor = Application.Context.ContentResolver.Query(CalendarContract.Events.ContentUri, eventProperties, $"{CalendarContract.Events.InterfaceConsts.Dtstart} > ? AND {CalendarContract.Events.InterfaceConsts.Dtstart} <= ?", new string[] { last.ToString(), now.ToString() }, CalendarContract.Events.InterfaceConsts.Dtstart + " DESC");
			Dictionary<string, int> columns = eventProperties.ToDictionary(x => x, x => cursor.GetColumnIndex(x));

			while (cursor.MoveToNext())
			{
				CalendarDatum calendarDatum = new CalendarDatum(cursor.GetString(columns[CalendarContract.Events.InterfaceConsts.Id]), cursor.GetString(columns[CalendarContract.Events.InterfaceConsts.Title]), epoch.AddMilliseconds(cursor.GetLong(columns[CalendarContract.Events.InterfaceConsts.Dtstart])), epoch.AddMilliseconds(cursor.GetLong(columns[CalendarContract.Events.InterfaceConsts.Dtend])), cursor.GetDouble(columns[CalendarContract.Events.InterfaceConsts.Duration]), cursor.GetString(columns[CalendarContract.Events.InterfaceConsts.Description]), cursor.GetString(columns[CalendarContract.Events.InterfaceConsts.EventLocation]), cursor.GetString(columns[CalendarContract.Events.InterfaceConsts.Organizer]), cursor.GetInt(columns[CalendarContract.Events.InterfaceConsts.IsOrganizer]) == 1, DateTimeOffset.UtcNow);

				calendarDatums.Add(calendarDatum);
			}

			return calendarDatums;
		}
	}
}

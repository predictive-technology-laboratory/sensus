using Newtonsoft.Json;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Plugin.Permissions.Abstractions;
using Android.Provider;
using Android.Content;
using Android.Database;
using Android.App;

namespace Sensus.Android.Probes.Apps
{
    public class AndroidCalendarProbe : CalendarProbe
    {
        public AndroidCalendarProbe()
        {

        }
        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

        }

        protected async override Task<List<CalendarDatum>> GetCalendarEventsAsync()
        {
            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Calendar) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Calendar permission is not permitted on this device. Cannot start Calendar probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

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

            long now = Java.Lang.JavaSystem.CurrentTimeMillis();
            long lastPoll = now - PollingSleepDurationMS;

            global::Android.Net.Uri eventsUri = CalendarContract.Events.ContentUri;

            ICursor cursor = Application.Context.ContentResolver.Query(eventsUri, eventProperties, $"{CalendarContract.Events.InterfaceConsts.Dtstart} > ? AND {CalendarContract.Events.InterfaceConsts.Dtstart} <= ?", new string[] { lastPoll.ToString(), now.ToString() }, CalendarContract.Events.InterfaceConsts.Dtstart + " DESC");


            while (cursor.MoveToNext())
            {
                CalendarDatum calendarDatum = new CalendarDatum(DateTimeOffset.UtcNow)
                {
                    Id = cursor.GetString(0),
                    Title = cursor.GetString(1),
                    Start = cursor.GetString(2),
                    End = cursor.GetString(3),
                    Duration = cursor.GetDouble(4),
                    Description = cursor.GetString(5),
                    EventLocation = cursor.GetString(6),
                    Organizer = cursor.GetString(7),
                    IsOrganizer = cursor.GetString(8) == "true" ? true : false

                };

                calendarDatums.Add(calendarDatum);
            }

            return calendarDatums;
        }
    }
}

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
        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
        }

        protected async override Task<List<Datum>> GetCalendarEvents()
        {

            // BLE requires location permissions
            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Calendar) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Calendar permission is not permitted on this device. Cannot start Calendar probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

            global::Android.Net.Uri eventsUri = CalendarContract.Events.ContentUri;

            string[] eventsProjection = {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                //CalendarContract.Events.InterfaceConsts.
            };

            return null;
        }
    }
}

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.V4.Content;
using Java.Lang;
using SensusService;
using System.IO;
using Xamarin;
using Xamarin.Geolocation;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        public const string INTENT_EXTRA_NAME_PING = "PING-SENSUS";

        private ConnectivityManager _connectivityManager;
        private readonly string _deviceId;
        private Activity _mainActivity;

        public Activity MainActivity
        {
            get { return _mainActivity; }
            set { _mainActivity = value; }
        }

        public override bool WiFiConnected
        {
            get { return _connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected; }
        }

        public override bool IsCharging
        {
            get
            {
                IntentFilter filter = new IntentFilter(Intent.ActionBatteryChanged);
                BatteryStatus status = (BatteryStatus)Application.Context.RegisterReceiver(null, filter).GetIntExtra(BatteryManager.ExtraStatus, -1);
                return status == BatteryStatus.Charging || status == BatteryStatus.Full;
            }
        }

        public override string DeviceId
        {
            get { return _deviceId; }
        }

        public AndroidSensusServiceHelper()
            : base(new Geolocator(Application.Context))
        {
            _connectivityManager = Application.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            _deviceId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }

        protected override void InitializeXamarinInsights()
        {
            Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY, Application.Context);
        }

        public override void ShareFile(string path, string subject)
        {
            try
            {
                Intent intent = new Intent(Intent.ActionSend);
                intent.SetType("text/plain");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                if (!string.IsNullOrWhiteSpace(subject))
                    intent.PutExtra(Intent.ExtraSubject, subject);

                Java.IO.File file = new Java.IO.File(path);
                Uri uri = FileProvider.GetUriForFile(Application.Context, "edu.virginia.sie.ptl.sensus.fileprovider", file);
                intent.PutExtra(Intent.ExtraStream, uri);

                // if we're outside the context of the main activity (e.g., within the service), start a new activity
                if (_mainActivity == null)
                {
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                }
                // otherwise, start the activity off the main activity -- smoother transition back to sensus after the file has been shared
                else
                    _mainActivity.StartActivity(intent);
            }
            catch (Exception ex) { Logger.Log("Failed to start intent to share file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal); }
        }

        protected override void StartSensusPings(int ms)
        {
            SetSensusMonitoringAlarm(ms);
        }

        protected override void StopSensusPings()
        {
            SetSensusMonitoringAlarm(-1);
        }

        private void SetSensusMonitoringAlarm(int ms)
        {
            Context context = Application.Context;
            AlarmManager alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            Intent serviceIntent = new Intent(context, typeof(AndroidSensusService));
            serviceIntent.PutExtra(INTENT_EXTRA_NAME_PING, true);
            PendingIntent pendingServiceIntent = PendingIntent.GetService(context, 0, serviceIntent, PendingIntentFlags.UpdateCurrent);

            if (ms > 0)
                alarmManager.SetRepeating(AlarmType.RtcWakeup, JavaSystem.CurrentTimeMillis() + ms, ms, pendingServiceIntent);
            else
                alarmManager.Cancel(pendingServiceIntent);
        }
    }
}
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.Lang;
using SensusService;
using System.IO;
using Xamarin.Geolocation;
using Application = Android.App.Application;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        private static string _preventAutoRestartPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "no_autorestart");
        private static bool _autoRestart = PendingIntent.GetBroadcast (Application.Context, 0, GetAutoRestartIntent(Application.Context), PendingIntentFlags.NoCreate) != null;

        private static Intent GetAutoRestartIntent(Context context)
        {
            return new Intent(context, typeof(AndroidSensusService));
        }

        private static PendingIntent GetAutoRestartPendingIntent(Context context)
        {
            return PendingIntent.GetService(context, 0, GetAutoRestartIntent(context), PendingIntentFlags.UpdateCurrent);
        }

        public static void UpdateAutoRestart(Context context, bool enable)
        {
            _autoRestart = enable;

            AlarmManager alarm = context.GetSystemService(Context.AlarmService) as AlarmManager;

            PendingIntent pendingIntent = GetAutoRestartPendingIntent(context);

            if (_autoRestart && !File.Exists(_preventAutoRestartPath))
            {
                long nextAlarmMS = JavaSystem.CurrentTimeMillis() + 5000;
                alarm.SetRepeating(AlarmType.RtcWakeup, nextAlarmMS, 1000 * 60, pendingIntent);
                Toast.MakeText(context, "Sensus auto-restart has been enabled.", ToastLength.Long).Show();
            }
            else
            {
                alarm.Cancel(pendingIntent);
                Toast.MakeText(context, "Sensus auto-restart has been disabled.", ToastLength.Long).Show();
            }
        }

        private ConnectivityManager _connectivityManager;
        private string _deviceId;

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

        public override bool AutoRestart
        {
            get { return _autoRestart; }
            set
            {
                if (value && File.Exists(_preventAutoRestartPath))
                    File.Delete(_preventAutoRestartPath);
                else if (!value && !File.Exists(_preventAutoRestartPath))
                    File.Create(_preventAutoRestartPath);
                    
                if (value != _autoRestart)
                {
                    UpdateAutoRestart(Application.Context, value);
                    OnPropertyChanged();
                }
            }
        }

        public AndroidSensusServiceHelper()
            : base(new Geolocator(Application.Context))
        {
            _connectivityManager = Application.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            _deviceId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }
    }
}
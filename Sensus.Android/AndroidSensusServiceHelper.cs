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

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        private static string _preventAutoRestartPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "no_autorestart");

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

        public AndroidSensusServiceHelper()
            : base(new Geolocator(Application.Context), !File.Exists(_preventAutoRestartPath))
        {
            _connectivityManager = Application.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            _deviceId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }

        public override void ShareProtocol(Protocol protocol, Protocol.ShareMethod method)
        {
            Intent intent = null;

            if (method == Protocol.ShareMethod.Email)
            {
                intent = new Intent(Intent.ActionSend);
                intent.PutExtra(Intent.ExtraSubject, "Sensus Protocol:  " + protocol.Name);
                intent.PutExtra(Intent.ExtraEmail, new string[] { "gerber.matthew@gmail.com" });
                intent.AddFlags(ActivityFlags.NewTask);
                intent.SetType("message/rfc822");

                string path = Path.GetTempFileName();
                protocol.Save(path);
                Java.IO.File file = new Java.IO.File(path);
                file.SetReadable(true, true);
                global::Android.Net.Uri uri = global::Android.Net.Uri.FromFile(file);
                intent.PutExtra(Intent.ExtraStream, uri);
            }

            if (intent != null)
                Application.Context.StartActivity(intent);
        }

        protected override void SetAutoRestart(bool enabled)
        {
            Context context = Application.Context;
            AlarmManager alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            Intent serviceIntent = new Intent(context, typeof(AndroidSensusService));
            PendingIntent pendingServiceIntent = PendingIntent.GetService(context, 0, serviceIntent, PendingIntentFlags.UpdateCurrent);

            if (enabled)
            {
                if (File.Exists(_preventAutoRestartPath))
                    File.Delete(_preventAutoRestartPath);

                long nextAlarmMS = JavaSystem.CurrentTimeMillis() + 5000;
                alarmManager.SetRepeating(AlarmType.RtcWakeup, nextAlarmMS, 1000 * 60, pendingServiceIntent);
                Toast.MakeText(context, "Sensus auto-restart has been enabled.", ToastLength.Long).Show();
            }
            else
            {
                if (!File.Exists(_preventAutoRestartPath))
                    File.Create(_preventAutoRestartPath);

                alarmManager.Cancel(pendingServiceIntent);
                Toast.MakeText(context, "Sensus auto-restart has been disabled.", ToastLength.Long).Show();
            }
        }
    }
}
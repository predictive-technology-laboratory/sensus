using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Application = Android.App.Application;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Geolocation;
using System.Threading.Tasks;
using Android.Net;
using Sensus.UI.Properties;
using System.ComponentModel;
using Android.Provider;

namespace Sensus.Android
{
    public class AndroidApp : App
    {
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
            get
            {
                if (_deviceId == null)
                    _deviceId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);

                return _deviceId;
            }
        }

        public AndroidApp()
            : base(new Geolocator(Application.Context))
        {
            Task.Run(() =>
                {
                    _connectivityManager = Application.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;

                    // start service -- if it's already running from on-boot startup, this will have no effect
                    Intent serviceIntent = new Intent(Application.Context, typeof(AndroidSensusService));
                    Application.Context.StartService(serviceIntent);

                    // bind to the service
                    SensusServiceConnection serviceConnection = new SensusServiceConnection(null);
                    serviceConnection.ServiceConnected += (o, e) =>
                        {
                            SensusService = e.Binder.Service;  // set service within App
                        };

                    Application.Context.BindService(serviceIntent, serviceConnection, Bind.AutoCreate);
                });
        }
    }
}
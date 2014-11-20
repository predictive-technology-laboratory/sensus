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

namespace Sensus.Android
{
    public class AndroidApp : App
    {
        public static void Initialize(Geolocator locator)
        {
            Set(new AndroidApp(locator));
        }

        protected AndroidApp(Geolocator locator)
            : base(locator)
        {
            Task.Run(() =>
                {
                    // start service -- if it's already running from on-boot startup, this will have no effect
                    Intent serviceIntent = new Intent(Application.Context, typeof(AndroidSensusService));
                    Application.Context.StartService(serviceIntent);

                    // bind to the service
                    SensusServiceConnection serviceConnection = new SensusServiceConnection(null);
                    serviceConnection.ServiceConnected += (o, e) =>
                        {
                            SensusService = e.Binder.Service;  // bind

                            if (App.LoggingLevel >= LoggingLevel.Normal)
                                App.Get().SensusService.Log("Connected to Sensus service.");
                        };

                    Intent bindServiceIntent = new Intent(Application.Context, typeof(AndroidSensusService));
                    Application.Context.BindService(bindServiceIntent, serviceConnection, Bind.AutoCreate);
                });
        }
    }
}
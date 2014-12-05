using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SensusService;
using SensusUI;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Geolocation;

namespace Sensus.Android
{
    [Activity(Label = "Sensus", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            Title = "Loading Sensus...";

            // start service -- if it's already running, this will have no effect
            Intent serviceIntent = new Intent(Application.Context, typeof(AndroidSensusService));
            Application.Context.StartService(serviceIntent);

            // bind UI to the service
            AndroidSensusServiceConnection serviceConnection = new AndroidSensusServiceConnection(null);
            serviceConnection.ServiceConnected += (o, e) =>
                {
                    UiBoundSensusServiceHelper.Set(e.Binder.Service.SensusServiceHelper);
                };

            Application.Context.BindService(serviceIntent, serviceConnection, Bind.AutoCreate);

            // stop activity when user presses stop
            MainPage.StopSensusTapped += (o, e) => { Finish(); };

            SetPage(new SensusNavigationPage());

            Title = "Sensus";
        }
    }
}


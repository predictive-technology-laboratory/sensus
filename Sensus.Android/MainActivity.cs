using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Sensus.UI;
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

            Application.Context.StartService(new Intent(Application.Context, typeof(AndroidSensusService)));  // start service -- if it's already running from on-boot or on-timer startup, this will have no effect

            Task.Run(() => { SensusServiceHelper.Get().Stopped += (o, e) => { Finish(); }; });  // end activity when the service stops

            SetPage(new SensusNavigationPage());

            Title = "Sensus";
        }
    }
}


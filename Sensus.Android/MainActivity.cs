using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Sensus.UI;
using System;
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

            // start service -- if it's already running from on-boot startup, this will have no effect
            Intent serviceIntent = new Intent(Application.Context, typeof(AndroidSensusService));
            Application.Context.StartService(serviceIntent);

            MainPage.StopSensusTapped += (o, e) => { Finish(); };  // end activity when the user taps stop

            SetPage(new SensusNavigationPage());

            Title = "Sensus";
        }
    }
}


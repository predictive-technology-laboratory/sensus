using Android.App;
using Android.Content.PM;
using Android.OS;
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

            AndroidApp app = new AndroidApp();

            app.StopSensusTapped += (o, e) => { Finish(); };  // end activity when the user taps stop

            SetPage(app.NavigationPage);

            Title = "Sensus";
        }
    }
}


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

            // perform platfor-specific initialization and bind to stop event
            AndroidApp.Initialize(new Geolocator(this));
            App.Get().StopSensusTapped += (o, e) => { Finish(); };

            // show start screen
            SetPage(App.Get().NavigationPage);

            Title = "Sensus";
        }
    }
}


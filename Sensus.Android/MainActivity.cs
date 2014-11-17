using Android.App;
using Android.Content.PM;
using Android.OS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Geolocation;

namespace Sensus.Android
{
    [Activity(Label = "Loading Sensus...", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            AndroidApp.Initialize(new Geolocator(this), this);

            SetPage(App.Get().NavigationPage);
        }

        protected override void OnStop()
        {
            base.OnStop();

            App.Get().OnStop();
        }
    }
}


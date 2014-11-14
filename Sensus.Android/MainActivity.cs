using Android.App;
using Android.Content.PM;
using Android.OS;
using Sensus.Android.Probes;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Android
{
    [Activity(Label = "Loading Sensus...", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            App.Init(new AndroidProbeInitializer(this));
            Forms.Init(this, bundle);

            SetPage(App.Get().NavigationPage);
        }
    }
}


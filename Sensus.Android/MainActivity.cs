using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;
using Sensus.UI;
using Sensus.Probes;
using Sensus.Android.Probes;

namespace Sensus.Android
{
    [Activity(Label = "Loading Sensus...", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            ProbeInitializer.Set(new AndroidProbeInitializer(this));

            Xamarin.Forms.Forms.Init(this, bundle);

            SetPage(new MainPage());
        }
    }
}


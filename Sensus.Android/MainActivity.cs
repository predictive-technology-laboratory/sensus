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
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "http", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "file", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]
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
            serviceConnection.ServiceConnected += async (o, e) =>
                {
                    UiBoundSensusServiceHelper.Set(e.Binder.SensusServiceHelper);

                    UiBoundSensusServiceHelper.Get().Stopped += (oo, ee) => { Finish(); };  // stop activity when service stops

                    SensusNavigationPage navigationPage = new SensusNavigationPage();

                    SetPage(navigationPage);

                    // open page to view protocol if a protocol was passed to us
                    if (Intent.Data != null)
                    {
                        global::Android.Net.Uri dataURI = Intent.Data;

                        Protocol protocol = null;
                        if (Intent.Scheme == "http")
                            protocol = await Protocol.GetFromWeb(dataURI.ToString());
                        else if (Intent.Scheme == "file")
                            protocol = await Protocol.GetFromFile(dataURI.Path);

                        if (protocol != null)
                            await navigationPage.PushAsync(new ProtocolPage(protocol));
                    }

                    Title = "Sensus";
                };

            Application.Context.BindService(serviceIntent, serviceConnection, Bind.AutoCreate);
        }
    }
}
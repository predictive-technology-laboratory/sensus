using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensus.Probes.Apps;
using Syncfusion.SfChart.XForms;
using Android.AccessibilityServices;
using Android.Views.Accessibility;
using Android.Content;
using Android.App;
using Xamarin.Android;
using Android.Provider;

namespace Sensus.Android.Probes.Apps
{
    public class AndroidKeystrokeProbe : KeystrokeProbe
    {
        private EventHandler<KeystrokeDatum> _accessibilityCallback;

        public AndroidKeystrokeProbe()
        {
            _accessibilityCallback = async (sender, incomingKeystrokedatum) =>
            {
              await StoreDatumAsync(incomingKeystrokedatum);
            };

        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override async Task StartListeningAsync()
        {
            
            if (! isAccessibilityServiceEnabled()) {

                var response = await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Permission Request", "On the next screen, please enable the accessbility service permission to Sensus", "ok", "cancel");

                if (response)
                {
                    //user click ok 
                    Intent intent = new Intent(Settings.ActionAccessibilitySettings);
                    Application.Context.StartActivity(intent);

                }

            }

            AndroidKeystrokeService.AccessibilityBroadcast += _accessibilityCallback;
            //This doesn't work because the accessibility service can be started and stopped only by system apps
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StartService(serviceIntent);
            //return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            AndroidKeystrokeService.AccessibilityBroadcast -= _accessibilityCallback;
            //This doesn't work
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StopService(serviceIntent);
            return Task.CompletedTask;
        }
        public Boolean isAccessibilityServiceEnabled()
        {
            AccessibilityManager accessibilityManager = (AccessibilityManager)Application.Context.GetSystemService(AccessibilityService.AccessibilityService);
            IList<AccessibilityServiceInfo> enabledServices = accessibilityManager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask);
            bool check = false;
            for (int i = 0; i < enabledServices.Count; i++)
            {
                AccessibilityServiceInfo e = enabledServices[i];
                if (e.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName)
                {
                    check = true;
                }
            }
            return check;
        }


    }
}

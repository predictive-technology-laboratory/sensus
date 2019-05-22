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


namespace Sensus.Android.Probes.Apps
{
    public class AndroidKeystrokeProbe : KeystrokeProbe
    {
        private AndroidKeystrokeService _accessibilityListener;
        private DateTime? _accessibilityEventTime;
        private EventHandler<KeystrokeDatum> _accessibilityCallback;

        public AndroidKeystrokeProbe()
        {
            _accessibilityCallback = async (sender, incomingKeystrokedatum) =>
            {
                //_outgoingIncomingTime = DateTime.Now;
                Console.WriteLine("***** OnAccessibilityEvent Probeeeeeeeeeeeeeeeeeee***** " + incomingKeystrokedatum.Key + " " + incomingKeystrokedatum.App);

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
            //SensusServiceHelper.Get().FlashNotificationAsync("arrive");

            if (!isAccessibilityServiceEnabled()) {
                
               
                var response = await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Permission Request", "On the next screen, please enable the accessbility service permission for Sensus", "ok", "cancel");

                if (response)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("starttttttttttttttttt");
                    Intent intent = new Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS);
                    Application.Context.StartActivity(intent);
                    //user click ok  
                    Bluetooth
                }
                Xamarin.Android.App

            }

            AndroidKeystrokeService.AccessibilityBroadcast += _accessibilityCallback;
            //This doesn't work because the accessibility service can be started and stopped only by system apps
            Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            Application.Context.StartService(serviceIntent);
            //return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            AndroidKeystrokeService.AccessibilityBroadcast += _accessibilityCallback;
            //This doesn't work
            Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            Application.Context.StopService(serviceIntent);
            return Task.CompletedTask;
        }

        public Boolean  isAccessibilityServiceEnabled()
        {
            AccessibilityManager accessibilityManager = (AccessibilityManager)Application.Context.GetSystemService(AccessibilityService.AccessibilityService);
            IList<AccessibilityServiceInfo> enabledServices = accessibilityManager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask);

            //SensusServiceHelper.Get().FlashNotificationAsync(enabledServices.Count.ToString());

            for (int i = 0; i < enabledServices.Count; i++) {
                AccessibilityServiceInfo e = enabledServices[i];
                Console.WriteLine("***** OnAccessibilityEvent ***** " + e.ResolveInfo.ServiceInfo.PackageName + "  " + Application.Context.ApplicationInfo.PackageName);
                if (e.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName) { 
                SensusServiceHelper.Get().FlashNotificationAsync(e.ResolveInfo.ServiceInfo.PackageName.ToString());
                return true;
                }
            }

            //foreach (AccessibilityServiceInfo enabled in enabledServices)
            //{
            //    //SensusServiceHelper.Get().FlashNotificationAsync(enabled.ResolveInfo.ServiceInfo.PackageName + "  " + Application.Context.ApplicationInfo.PackageName);
            //    //Console.WriteLine("***** OnAccessibilityEvent ***** " + enabled.ResolveInfo.ServiceInfo.PackageName + "  " + Application.Context.ApplicationInfo.PackageName);

            //    if (enabled.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName)
            //        SensusServiceHelper.Get().FlashNotificationAsync(enabled.ResolveInfo.ServiceInfo.PackageName.ToString());
            //    return true;
            //}
            

            return false;
        }
    }
}

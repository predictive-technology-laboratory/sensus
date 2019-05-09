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
using Android.Views.Accessibility;
using System.Diagnostics;
using Android.Text;
using System.Runtime.Remoting.Contexts;

namespace Sensus.Android.Probes.Apps
{
    public class AndroidKeystrokeProbe : KeystrokeProbe
    {
        private AndroidKeystrokeService _accessibilityListener;
        private DateTime? _accessibilityEventTime;

        public AndroidKeystrokeProbe()
        {
            _accessibilityListener = new AndroidKeystrokeService();

            _accessibilityListener.AccessibilityBroadcast += async (o, key) =>
            {
                _accessibilityEventTime = DateTime.Now;
                Console.WriteLine("***** OnAccessibilityEvent ***** eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee ");

                await SensusServiceHelper.Get().FlashNotificationAsync(key);
                //await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.IncomingCall, incomingNumber, null));
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

        protected override Task StartListeningAsync()
        {
            //SensusServiceHelper.Get().FlashNotificationAsync("arrive");

            Boolean b = isAccessibilityServiceEnabled();
            Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            Application.Context.StartService(serviceIntent);
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
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

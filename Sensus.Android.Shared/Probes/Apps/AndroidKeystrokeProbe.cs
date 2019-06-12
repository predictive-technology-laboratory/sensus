﻿using System;
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
                
                //Console.WriteLine("***** OnAccessibilityEvent Probeeeeeeeeeeeeeeeeeee***** " + incomingKeystrokedatum.Key + " " + incomingKeystrokedatum.App);

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

        protected override  Task StartListeningAsync()
        {

            if (!isAccessibilityServiceEnabled()) {

                //This temporary, code should be added to automatically open settings page.
                 //Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Permission Request", "Please navigate to settings->Accessibility and enable the accessbility service permission for Sensus", "ok", "cancel");

                //var response = await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Permission Request", "On the next screen, please enable the accessbility service permission for Sensus", "ok", "cancel");

                //if (response)
                //{
                //    //user click ok 
                //    //await SensusServiceHelper.Get().FlashNotificationAsync("starttttttttttttttttt");
                //    //Intent intent = new Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS);
                //    //Application.Context.StartActivity(intent);

                //}

            }

            AndroidKeystrokeService.AccessibilityBroadcast += _accessibilityCallback;
            //This doesn't work because the accessibility service can be started and stopped only by system apps
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StartService(serviceIntent);
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            AndroidKeystrokeService.AccessibilityBroadcast -= _accessibilityCallback;
            //This doesn't work
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StopService(serviceIntent);
            return Task.CompletedTask;
        }

        public Boolean  isAccessibilityServiceEnabled()
        {
            AccessibilityManager accessibilityManager = (AccessibilityManager)Application.Context.GetSystemService(AccessibilityService.AccessibilityService);
            IList<AccessibilityServiceInfo> enabledServices = accessibilityManager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask);
            bool check = false;
            for (int i = 0; i < enabledServices.Count; i++) {
                AccessibilityServiceInfo e = enabledServices[i];
                if (e.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName) { 
                check = true;
                }
            }
            return check;
        }
    }
}

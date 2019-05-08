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

namespace Sensus.Android.Probes.Apps
{
    public class AndroidKeystrokeProbe : KeystrokeProbe
    {
        public AndroidKeystrokeProbe()
        {
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
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StartService(serviceIntent);
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            //Intent serviceIntent = new Intent(Application.Context, typeof(AndroidKeystrokeService));
            //Application.Context.StopService(serviceIntent);
            return Task.CompletedTask;
        }
    }
}

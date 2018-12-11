//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Movement
{
    /// <summary>
    /// Provides acceleration in x, y, and z directions as <see cref="AccelerometerDatum"/> readings.
    /// </summary>
    public abstract class AccelerometerProbe : ListeningProbe
    {
        private bool _stabilizing;

        protected bool Stabilizing
        {
            get { return _stabilizing; }
        }

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return true;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will use additional power to report all updates.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will sleep and pause updates.";
            }
        }

        public sealed override string DisplayName
        {
            get { return "Acceleration"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(AccelerometerDatum); }
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _stabilizing = true;
        }

        protected override async Task StartListeningAsync()
        {
            // allow the accelerometer to stabilize...the first few readings can be extremely erratic
            await Task.Delay(2000);
            _stabilizing = false;

            // not sure if null is the problem:  https://insights.xamarin.com/app/Sensus-Production/issues/907
            if (SensusServiceHelper.Get() != null)
            {
                SensusServiceHelper.Get().Logger.Log("Accelerometer has finished stabilization period.", LoggingLevel.Normal, GetType());
            }
        }

        public override async Task ResetAsync()
        {
            await base.ResetAsync();

            _stabilizing = false;
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
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
    }
}

// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
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
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
using Plugin.Permissions.Abstractions;
using Sensus.UI.UiProperties;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.Probes.Context
{
    public abstract class SoundProbe : PollingProbe
    {
        private int _sampleLengthMS;

        /// <summary>
        /// How many milliseconds to sample audio from the microphone when computing the 
        /// decibel level.
        /// </summary>
        /// <value>The sample length ms.</value>
        [EntryIntegerUiProperty("Sample Length (MS):", true, 5, true)]
        public int SampleLengthMS
        {
            get { return _sampleLengthMS; }
            set { _sampleLengthMS = value; }
        }

        public sealed override string DisplayName
        {
            get { return "Sound Level"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 60000 * 10; // every 10 minutes
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(SoundDatum); }
        }

        protected SoundProbe()
        {
            _sampleLengthMS = 5000;
        }

        protected override async Task ProtectedInitializeAsync()
        {
            await base.ProtectedInitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Microphone) != PermissionStatus.Granted)
            {
                string error = "Microphone use is not permitted on this device. Cannot start sound probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as SoundDatum).Decibels);
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new DateTimeAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Time"
                }
            };
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Decibels"
                }
            };
        }
    }
}

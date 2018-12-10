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

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

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

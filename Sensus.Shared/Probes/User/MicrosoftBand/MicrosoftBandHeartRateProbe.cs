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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandHeartRateProbe : MicrosoftBandProbe<BandHeartRateSensor, BandHeartRateReading>
    {
        public override Type DatumType
        {
            get
            {
                return typeof(MicrosoftBandHeartRateDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Microsoft Band Heart Rate";
            }
        }

        protected override BandHeartRateSensor GetSensor(BandClient bandClient)
        {
            return bandClient.SensorManager.HeartRate;
        }

        protected override async Task StartReadingsAsync()
        {
            if (Sensor.UserConsented == UserConsent.Unspecified)
            {
                await Sensor.RequestUserConsent();

                if (Sensor.UserConsented != UserConsent.Granted)
                {
                    throw new Exception("User did not consent.");
                }
            }

            if (Sensor.UserConsented == UserConsent.Granted)
            {
                await base.StartReadingsAsync();
            }
        }

        protected override Datum GetDatumFromReading(BandHeartRateReading reading)
        {
            return new MicrosoftBandHeartRateDatum(DateTimeOffset.UtcNow, reading.HeartRate);
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as MicrosoftBandHeartRateDatum).HeartRate);
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Heart Rate"
                }
            };
        }
    }
}
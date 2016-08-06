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

namespace SensusService.Probes.User.Scripts.MicrosoftBand
{
    public class MicrosoftBandRRIntervalProbe : MicrosoftBandProbe<BandRRIntervalSensor, BandRRIntervalReading>
    {
        public override Type DatumType
        {
            get
            {
                return typeof(MicrosoftBandRRIntervalDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Microsoft Band R-R Interval";
            }
        }

        protected override BandRRIntervalSensor GetSensor(BandClient bandClient)
        {
                return bandClient.SensorManager.RRInterval;
        }

        protected override void StartReadings()
        {
            if (Sensor.UserConsented == UserConsent.Unspecified)
            {
                ManualResetEvent consentWait = new ManualResetEvent(false);
                Task.Run(async () =>
                {
                    await Sensor.RequestUserConsent();
                    consentWait.Set();
                });

                consentWait.WaitOne();

                if (Sensor.UserConsented != UserConsent.Granted)
                    throw new Exception("User did not consent.");
            }

            if (Sensor.UserConsented == UserConsent.Granted)
                base.StartReadings();
        }

        protected override Datum GetDatumFromReading(BandRRIntervalReading reading)
        {
            return new MicrosoftBandRRIntervalDatum(DateTimeOffset.UtcNow, reading.Interval);
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as MicrosoftBandRRIntervalDatum).Interval);
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "R-R Interval"
                }
            };
        }
    }
}
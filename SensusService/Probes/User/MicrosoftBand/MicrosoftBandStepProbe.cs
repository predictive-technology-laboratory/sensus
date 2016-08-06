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
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using Syncfusion.SfChart.XForms;

namespace SensusService.Probes.User.Scripts.MicrosoftBand
{
    public class MicrosoftBandStepProbe : MicrosoftBandProbe<BandAltimeterSensor, BandAltimeterReading>
    {
        public override Type DatumType
        {
            get
            {
                return typeof(MicrosoftBandStepDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Microsoft Band Steps";
            }
        }

        protected override BandAltimeterSensor GetSensor(BandClient bandClient)
        {
            return bandClient.SensorManager.Altimeter;
        }

        protected override Datum GetDatumFromReading(BandAltimeterReading reading)
        {
            return new MicrosoftBandStepDatum(DateTimeOffset.UtcNow, reading.StepsAscended);
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as MicrosoftBandStepDatum).StepsAscended);
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Steps Ascended"
                }
            };
        }
    }
}
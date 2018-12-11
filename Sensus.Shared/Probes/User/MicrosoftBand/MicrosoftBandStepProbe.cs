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
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.User.MicrosoftBand
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

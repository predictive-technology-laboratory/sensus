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
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.User.MicrosoftBand
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

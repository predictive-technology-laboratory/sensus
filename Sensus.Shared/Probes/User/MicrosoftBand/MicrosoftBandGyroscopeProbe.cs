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
    public class MicrosoftBandGyroscopeProbe : MicrosoftBandProbe<BandGyroscopeSensor, BandGyroscopeReading>
    {
        public override Type DatumType
        {
            get
            {
                return typeof(MicrosoftBandGyroscopeDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Microsoft Band Gyroscope";
            }
        }

        protected override BandGyroscopeSensor GetSensor(BandClient bandClient)
        {
            return bandClient.SensorManager.Gyroscope;
        }

        protected override Datum GetDatumFromReading(BandGyroscopeReading reading)
        {
            return new MicrosoftBandGyroscopeDatum(DateTimeOffset.UtcNow, reading.AngularVelocityX, reading.AngularVelocityY, reading.AngularVelocityZ);
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return null;
        }
    }
}

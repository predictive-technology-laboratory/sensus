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
using Sensus;
using Sensus.Probes.User.Health;
using HealthKit;
using Xamarin.Forms.Platform.iOS;
using Syncfusion.SfChart.XForms;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitWeightProbe : iOSHealthKitSamplingProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Weight";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(WeightDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return (int)TimeSpan.FromDays(5).TotalMilliseconds;
            }
        }

        public iOSHealthKitWeightProbe()
            : base(HKQuantityType.Create(HKQuantityTypeIdentifier.BodyMass))
        {
        }

        protected override Datum ConvertSampleToDatum(HKSample sample)
        {
            HKQuantitySample quantitySample = sample as HKQuantitySample;

            if (quantitySample == null)
            {
                return null;
            }
            else
            {
                return new WeightDatum(new DateTimeOffset(quantitySample.StartDate.ToDateTime(), TimeSpan.Zero), quantitySample.Quantity.GetDoubleValue(HKUnit.Pound));
            }
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
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
                    Text = "Weight (Pounds)"
                }
            };
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as WeightDatum).WeightPounds);
        }
    }
}

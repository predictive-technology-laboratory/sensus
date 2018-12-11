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
using System.Collections.Generic;
using Sensus;
using System.Threading;
using HealthKit;
using Foundation;
using Sensus.Probes.User.Health;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBiologicalSexProbe : iOSHealthKitProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Biological Sex";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(BiologicalSexDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitBiologicalSexProbe()
            : base(HKCharacteristicType.Create(HKCharacteristicTypeIdentifier.BiologicalSex))
        {
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            HKBiologicalSexObject biologicalSex = HealthStore.GetBiologicalSex(out error);

            if (error == null)
            {
                if (biologicalSex.BiologicalSex == HKBiologicalSex.Female)
                {
                    data.Add(new BiologicalSexDatum(DateTimeOffset.UtcNow, BiologicalSex.Female));
                }
                else if (biologicalSex.BiologicalSex == HKBiologicalSex.Male)
                {
                    data.Add(new BiologicalSexDatum(DateTimeOffset.UtcNow, BiologicalSex.Male));
                }
                else if (biologicalSex.BiologicalSex == HKBiologicalSex.Other)
                {
                    data.Add(new BiologicalSexDatum(DateTimeOffset.UtcNow, BiologicalSex.Other));
                }
                else
                {
                    throw new Exception("User has not provided -- or has not allowed access to -- their biological sex.");
                }
            }
            else
            {
                throw new Exception("Error reading biological sex:  " + error.Description);
            }

            return Task.FromResult(data);
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

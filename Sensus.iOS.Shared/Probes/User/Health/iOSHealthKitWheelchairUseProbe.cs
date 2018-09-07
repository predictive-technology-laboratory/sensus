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

using HealthKit;
using Foundation;
using System;
using System.Threading;
using System.Collections.Generic;
using Sensus.Probes.User.Health;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitWheelChairUseProbe : iOSHealthKitProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Wheelchair Use";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(WheelChairUseDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitWheelChairUseProbe()
            : base(HKCharacteristicType.Create(HKCharacteristicTypeIdentifier.WheelchairUse))
        {
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            HKWheelchairUseObject wheelChair = HealthStore.GetWheelchairUse(out error);

            if (error == null)
            {
                if (wheelChair.WheelchairUse == HKWheelchairUse.NotSet)
                {
                    data.Add(new WheelChairUseDatum(DateTimeOffset.Now, WheelChairUse.NotSet));
                }
                else if (wheelChair.WheelchairUse == HKWheelchairUse.No)
                {
                    data.Add(new WheelChairUseDatum(DateTimeOffset.Now, WheelChairUse.No));
                }
                else if (wheelChair.WheelchairUse == HKWheelchairUse.Yes)
                {
                    data.Add(new WheelChairUseDatum(DateTimeOffset.Now, WheelChairUse.Yes));
                }
                else
                {
                    throw new Exception("User has not provided -- or has not allowed access to -- their wheel chair use status.");
                }
            }
            else
            {
                throw new Exception("Error reading wheel chair use status:  " + error.Description);
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
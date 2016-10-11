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
using Sensus.Shared;
using Sensus.Shared.Probes.User.Health;
using Syncfusion.SfChart.XForms;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBloodTypeProbe : iOSHealthKitProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Blood Type";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(BloodTypeDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitBloodTypeProbe()
            : base(HKCharacteristicType.Create(HKCharacteristicTypeIdentifier.BloodType))
        {
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            HKBloodTypeObject bloodType = HealthStore.GetBloodType(out error);

            if (error == null)
            {
                if (bloodType.BloodType == HKBloodType.ABNegative)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.ABNegative));
                else if (bloodType.BloodType == HKBloodType.ABPositive)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.ABPositive));
                else if (bloodType.BloodType == HKBloodType.ANegative)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.ANegative));
                else if (bloodType.BloodType == HKBloodType.APositive)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.APositive));
                else if (bloodType.BloodType == HKBloodType.BNegative)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.BNegative));
                else if (bloodType.BloodType == HKBloodType.BPositive)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.BPositive));
                else if (bloodType.BloodType == HKBloodType.ONegative)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.ONegative));
                else if (bloodType.BloodType == HKBloodType.OPositive)
                    data.Add(new BloodTypeDatum(DateTimeOffset.Now, BloodType.OPositive));
                else
                    throw new Exception("User has not provided -- or has not allowed access to -- their blood type.");
            }
            else
                throw new Exception("Error reading blood type:  " + error.Description);

            return data;
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
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
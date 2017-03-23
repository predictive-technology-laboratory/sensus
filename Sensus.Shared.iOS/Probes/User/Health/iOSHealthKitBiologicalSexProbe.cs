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
using System.Collections.Generic;
using Sensus;
using System.Threading;
using HealthKit;
using Foundation;
using Sensus.Probes.User.Health;
using Syncfusion.SfChart.XForms;

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

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            HKBiologicalSexObject biologicalSex = HealthStore.GetBiologicalSex(out error);

            if (error == null)
            {
                if (biologicalSex.BiologicalSex == HKBiologicalSex.Female)
                    data.Add(new BiologicalSexDatum(DateTimeOffset.Now, BiologicalSex.Female));
                else if (biologicalSex.BiologicalSex == HKBiologicalSex.Male)
                    data.Add(new BiologicalSexDatum(DateTimeOffset.Now, BiologicalSex.Male));
                else if (biologicalSex.BiologicalSex == HKBiologicalSex.Other)
                    data.Add(new BiologicalSexDatum(DateTimeOffset.Now, BiologicalSex.Other));
                else
                    throw new Exception("User has not provided -- or has not allowed access to -- their biological sex.");
            }
            else
                throw new Exception("Error reading biological sex:  " + error.Description);

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
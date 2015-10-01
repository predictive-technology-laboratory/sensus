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
using SensusService.Probes.User.Health;
using HealthKit;
using Xamarin.Forms.Platform.iOS;
using SensusService;
using Newtonsoft.Json;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitActiveEnergyProbe : iOSHealthKitSamplingProbe
    {
        protected override string DefaultDisplayName
        {
            get
            {
                return "Active Energy (HealthKit)";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(ActiveEnergyDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitActiveEnergyProbe()
            : base(HKObjectType.GetQuantityType(HKQuantityTypeIdentifierKey.ActiveEnergyBurned))
        {
        }

        protected override Datum ConvertSampleToDatum(HKSample sample)
        {
            HKQuantitySample quantitySample = sample as HKQuantitySample;

            if (quantitySample == null)
                return null;
            else
                return new ActiveEnergyDatum(new DateTimeOffset(quantitySample.StartDate.ToDateTime()), quantitySample.Quantity.GetDoubleValue(HKUnit.Calorie));
        }
    }
}

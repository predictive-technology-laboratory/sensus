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
using HealthKit;
using System.Collections.Generic;
using SensusService;
using System.Threading;
using Foundation;
using SensusService.Probes.User.Health;
using Newtonsoft.Json;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBloodTypeProbe : iOSHealthKitProbe
    {
        protected override string DefaultDisplayName
        {
            get
            {
                return "Blood Type (HealthKit)";
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
            : base(HKObjectType.GetCharacteristicType(HKCharacteristicTypeIdentifierKey.BloodType))
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
            }
            else
                SensusServiceHelper.Get().Logger.Log("Error reading blood type:  " + error.Description, LoggingLevel.Normal, GetType());

            return data;
        }
    }
}
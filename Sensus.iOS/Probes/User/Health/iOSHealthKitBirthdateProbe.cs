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
using SensusService;
using System.Threading;
using HealthKit;
using Foundation;
using Xamarin.Forms.Platform.iOS;
using Newtonsoft.Json;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBirthdateProbe : iOSHealthKitProbe
    {
        protected override string DefaultDisplayName
        {
            get
            {
                return "Birthdate (HealthKit)";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(BirthdateDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitBirthdateProbe()
            : base(HKObjectType.GetCharacteristicType(HKCharacteristicTypeIdentifierKey.DateOfBirth))
        {
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            NSDate dateOfBirth = HealthStore.GetDateOfBirth(out error);

            if (error == null)
                data.Add(new BirthdateDatum(DateTimeOffset.Now, new DateTimeOffset(dateOfBirth.ToDateTime())));
            else
                SensusServiceHelper.Get().Logger.Log("Error reading date of birth:  " + error.Description, LoggingLevel.Normal, GetType());

            return data;
        }
    }
}
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

using System.Threading;
using HealthKit;
using Foundation;
using Newtonsoft.Json;
using Sensus;
using Sensus.Probes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Sensus.iOS.Probes.User.Health
{
    public abstract class iOSHealthKitProbe : PollingProbe
    {
        private HKObjectType _objectType;
        private HKHealthStore _healthStore;

        [JsonIgnore]
        public HKObjectType ObjectType
        {
            get { return _objectType; }
        }

        [JsonIgnore]
        protected HKHealthStore HealthStore
        {
            get { return _healthStore; }
        }

        protected iOSHealthKitProbe(HKObjectType objectType)
        {
            _objectType = objectType;
            _healthStore = new HKHealthStore();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (HKHealthStore.IsHealthDataAvailable)
            {
                NSSet objectTypesToRead = NSSet.MakeNSObjectSet<HKObjectType>(new HKObjectType[] { ObjectType });

                Tuple<bool, NSError> successError = await _healthStore.RequestAuthorizationToShareAsync(new NSSet(), objectTypesToRead);

                if (!successError.Item1)
                {
                    string message = "Failed to request HealthKit authorization:  " + (successError.Item2?.ToString() ?? "[no details]");
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                    throw new Exception(message);
                }
            }
        }
    }
}
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
using Sensus.Shared;
using Sensus.Shared.Probes;

namespace Sensus.Shared.iOS.Probes.User.Health
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

        protected override void Initialize()
        {
            base.Initialize();

            // all HealthKit permissions were requested in a single batch when the protocol was started; however, HealthKit probes
            // can also be started individually after the protocol is started by toggling Enabled on the probe page. in such cases
            // we want to request permission for the HealthKit probe that was just started. if the permission associated with this
            // probe was already requested, these calls will have no effect. even if the user previously denied access for this probe
            // these calls will not do anything by design by iOS. however, if the user has never been prompted for the permission
            // associated with this probe (i.e., the user is starting the probe by toggling Enabled), then the user will be prompted.
            if (HKHealthStore.IsHealthDataAvailable)
            {                
                NSSet objectTypesToRead = NSSet.MakeNSObjectSet<HKObjectType>(new HKObjectType[] { ObjectType });
                ManualResetEvent authorizationWait = new ManualResetEvent(false);
                _healthStore.RequestAuthorizationToShare(new NSSet(), objectTypesToRead,
                    (success, error) =>
                    {
                        if (error != null)
                            SensusServiceHelper.Get().Logger.Log("Error while requesting HealthKit authorization:  " + error.Description, LoggingLevel.Normal, GetType());

                        authorizationWait.Set();
                    });

                authorizationWait.WaitOne();
            }
        }
    }
}
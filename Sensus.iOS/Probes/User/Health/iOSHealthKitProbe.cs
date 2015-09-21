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
using SensusService.Probes;
using System.Collections.Generic;
using SensusService;
using System.Threading;
using HealthKit;
using Foundation;

namespace Sensus.iOS.Probes.User.Health
{
    public abstract class iOSHealthKitProbe : PollingProbe
    {
        private HKHealthStore _healthStore;

        protected HKHealthStore HealthStore
        {
            get { return _healthStore; }
        }

        public abstract HKObjectType ObjectType { get; }

        protected iOSHealthKitProbe()
        {
            _healthStore = new HKHealthStore();
        }

        protected override void Initialize()
        {
            base.Initialize();

            HKAuthorizationStatus authorizationStatus = _healthStore.GetAuthorizationStatus(ObjectType);

            if (authorizationStatus == HKAuthorizationStatus.NotDetermined)
                throw new Exception("User has not authorized " + ObjectType);
            else if (authorizationStatus == HKAuthorizationStatus.SharingDenied)
                throw new NotSupportedException("User has denied access to " + ObjectType);
        }
    }
}
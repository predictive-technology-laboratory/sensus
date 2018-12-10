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

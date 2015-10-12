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
using System.Threading;
using SensusService;
using Sensus.iOS.Probes.User.Health;
using HealthKit;

namespace Sensus.iOS.Probes.User.Health
{
    public abstract class iOSHealthKitSamplingProbe : iOSHealthKitProbe
    {
        private int _queryAnchor;

        public int QueryAnchor
        {
            get
            {
                return _queryAnchor;
            }
            set
            {
                _queryAnchor = value;
            }
        }

        public iOSHealthKitSamplingProbe(HKObjectType objectType)
            : base(objectType)
        {
            _queryAnchor = 0;
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            ManualResetEvent queryWait = new ManualResetEvent(false);
            Exception exception = null;

            HealthStore.ExecuteQuery(new HKAnchoredObjectQuery(ObjectType as HKSampleType, null, (nuint)_queryAnchor, nuint.MaxValue, new HKAnchoredObjectResultHandler2(
                        (query, samples, newQueryAnchor, error) =>
                        {
                            try
                            {
                                if (error == null)
                                {
                                    foreach (HKSample sample in samples)
                                    {
                                        Datum datum = ConvertSampleToDatum(sample);
                                        if (datum != null)
                                            data.Add(datum);
                                    }

                                    _queryAnchor = (int)newQueryAnchor;
                                }
                                else
                                    throw new Exception("Error while querying HealthKit for " + ObjectType + ":  " + error.Description);
                            }
                            catch (Exception ex)
                            {
                                exception = new Exception("Failed storing HealthKit samples:  " + ex.Message);
                            }
                            finally
                            {
                                queryWait.Set();
                            }
                        })));

            queryWait.WaitOne();

            if (exception != null)
                throw exception;

            if (data.Count == 0)
                throw new Exception("User has not provided -- or has not allowed access to -- any " + ObjectType + " information since last poll.");

            return data;
        }

        protected abstract Datum ConvertSampleToDatum(HKSample sample);
    }
}
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

using System;
using System.Threading;
using System.Collections.Generic;
using Sensus;
using HealthKit;
using System.Threading.Tasks;

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

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
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
                                {
                                    data.Add(datum);
                                }
                            }

                            _queryAnchor = (int)newQueryAnchor;
                        }
                        else
                        {
                            throw new Exception("Error while querying HealthKit for " + ObjectType + ":  " + error.Description);
                        }
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
            {
                throw exception;
            }

            // let the system know that we polled but didn't get any data
            if (data.Count == 0)
            {
                data.Add(null);
            }

            return Task.FromResult(data);
        }

        protected abstract Datum ConvertSampleToDatum(HKSample sample);
    }
}

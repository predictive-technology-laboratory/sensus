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
using System.Linq;

namespace Sensus.Probes
{
    public class DataRateCalculator
    {
        private TimeSpan _sampleDuration;
        private List<Datum> _sample;

        public double DataPerSecond
        {
            get
            {
                lock (_sample)
                {
                    // remove any data that are older than the sample duration
                    while (_sample.Count > 0 && (DateTimeOffset.Now - _sample.First().Timestamp).TotalSeconds > _sampleDuration.TotalSeconds)
                    {
                        _sample.RemoveAt(0);
                    }

                    // return data rate
                    if (_sample.Count > 1)
                    {
                        return _sample.Count / (_sample.Last().Timestamp - _sample.First().Timestamp).TotalSeconds;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public DataRateCalculator(TimeSpan sampleDuration)
        {
            _sampleDuration = sampleDuration;
            _sample = new List<Datum>();
        }

        public void Add(Datum datum)
        {
            // probes are allowed to generate null data (e.g., the POI probe indicating that no POI are nearby). for the purposes of data rate 
            // calculation, these should be ignored.
            if (datum != null)
            {
                // maintain a sample of the given duration
                lock (_sample)
                {
                    _sample.Add(datum);
                }
            }
        }
    }
}
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
        private int _sampleSize;
        private List<Datum> _sample;
        private long _totalAdded;

        public int SampleSize
        {
            get
            {
                return _sampleSize;
            }
        }

        public long TotalAdded
        {
            get
            {
                return _totalAdded;
            }
        }

        public double DataPerSecond
        {
            get
            {
                lock (_sample)
                {
                    TrimToSampleSize();

                    if (_sample.Count > 1)
                    {
                        return _sample.Count / (DateTimeOffset.UtcNow - _sample.First().Timestamp).TotalSeconds;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public DataRateCalculator(int sampleSize)
        {
            _sampleSize = sampleSize;
            _sample = new List<Datum>(_sampleSize);
            _totalAdded = 0;
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
                    _totalAdded++;
                    TrimToSampleSize();
                }
            }
        }

        private void TrimToSampleSize()
        {
            lock (_sample)
            {
                int numToRemove = _sample.Count - _sampleSize;
                if (numToRemove > 0)
                {
                    _sample.RemoveRange(0, numToRemove);
                }
            }
        }

        public void Clear()
        {
            lock (_sample)
            {
                _sample.Clear();
                _totalAdded = 0;
            }
        }
    }
}
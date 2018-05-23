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
using Sensus.Exceptions;

namespace Sensus.Probes
{
    /// <summary>
    /// A memoryless data rate calculator.
    /// </summary>
    public class DataRateCalculator
    {
        private enum SamplingModulusMatchAction
        {
            Store,
            Drop
        }

        public const float DATA_RATE_EPSILON = 0.00000000001f;

        private readonly long _sampleSize;
        private double? _maxDataStoresPerSecond;
        private DateTimeOffset? _startTimestamp;
        private long _dataCount;
        private double? _dataPerSecond;
        private int _samplingModulus;
        private SamplingModulusMatchAction _samplingModulusMatchAction;

        private readonly object _locker = new object();

        public double? DataPerSecond
        {
            get { return _dataPerSecond; }
        }

        public DataRateCalculator(long sampleSize, double? maxDataStoresPerSecond = null)
        {
            if (sampleSize <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSize), sampleSize, "Must be greater than zero.");
            }

            _sampleSize = sampleSize;
            _maxDataStoresPerSecond = maxDataStoresPerSecond;
        }

        public void Start(DateTimeOffset? startTimestamp = null)
        {
            lock (_locker)
            {
                _startTimestamp = startTimestamp ?? DateTimeOffset.UtcNow;

                _dataCount = 0;
                _dataPerSecond = null;

                // store all data to start with. we'll compute a store/drop rate after a data sample has been taken.
                _samplingModulus = 1;
                _samplingModulusMatchAction = SamplingModulusMatchAction.Store;
            }
        }

        public bool Add(Datum datum)
        {
            lock (_locker)
            {
                if (_startTimestamp == null)
                {
                    throw SensusException.Report("Data rate calculator has not been started.");
                }

                bool keepDatum = false;

                if (datum != null)
                {
                    double maxDataStoresPerSecond = _maxDataStoresPerSecond.GetValueOrDefault(float.MaxValue);

                    // non-negligible data per second:  check data rate
                    if (maxDataStoresPerSecond > DATA_RATE_EPSILON)
                    {
                        _dataCount++;

                        // check whether the current datum should be kept as part of sampling
                        bool isModulusMatch = (_dataCount % _samplingModulus) == 0;
                        if ((_samplingModulusMatchAction == SamplingModulusMatchAction.Store && isModulusMatch) ||
                            (_samplingModulusMatchAction == SamplingModulusMatchAction.Drop && !isModulusMatch))
                        {
                            keepDatum = true;
                        }

                        // recalculate data per second and sampling parameters
                        if (_dataCount == _sampleSize)
                        {
                            _dataPerSecond = _dataCount / (datum.Timestamp - _startTimestamp.Value).TotalSeconds;

                            #region recalculate the sampling modulus
                            double overagePerSecond = _dataPerSecond.Value - maxDataStoresPerSecond;

                            // if we're not over the limit then store all samples
                            if (overagePerSecond <= 0)
                            {
                                _samplingModulus = 1;
                                _samplingModulusMatchAction = SamplingModulusMatchAction.Store;
                            }
                            // otherwise calculate a modulus that will get as close as possible to the desired rate given the empirical rate
                            else
                            {
                                double samplingModulusMatchRate = overagePerSecond / _dataPerSecond.Value;
                                _samplingModulusMatchAction = SamplingModulusMatchAction.Drop;

                                if (samplingModulusMatchRate > 0.5)
                                {
                                    samplingModulusMatchRate = 1 - samplingModulusMatchRate;
                                    _samplingModulusMatchAction = SamplingModulusMatchAction.Store;
                                }

                                if (_samplingModulusMatchAction == SamplingModulusMatchAction.Store)
                                {
                                    // round the (store) modulus down to oversample -- more is better, right?
                                    _samplingModulus = (int)Math.Floor(1 / samplingModulusMatchRate);
                                }
                                else
                                {
                                    // round the (drop) modulus up to oversample -- more is better, right?
                                    _samplingModulus = (int)Math.Ceiling(1 / samplingModulusMatchRate);
                                }
                            }
                            #endregion

                            // start a new sample
                            _dataCount = 0;
                            _startTimestamp = datum.Timestamp;
                        }
                    }
                }

                return keepDatum;
            }
        }
    }
}
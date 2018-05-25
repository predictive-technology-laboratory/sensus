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
        /// <summary>
        /// Sampling action.
        /// </summary>
        public enum SamplingAction
        {
            /// <summary>
            /// Sample should be kept.
            /// </summary>
            Keep,

            /// <summary>
            /// Sample should be dropped
            /// </summary>
            Drop
        }

        public const float DATA_RATE_EPSILON = 0.00000001f;

        private long _sampleSize;
        private readonly long _originalSampleSize;
        private readonly double? _maxSamplesToKeepPerSecond;
        private DateTimeOffset? _startTimestamp;
        private long _dataCount;
        private double? _dataPerSecond;
        private long _samplingModulus;
        private SamplingAction _samplingModulusMatchAction;

        private readonly object _locker = new object();

        public DataRateCalculator(long sampleSize, double? maxSamplesToKeepPerSecond = null)
        {
            if (sampleSize <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSize), sampleSize, "Must be greater than 1.");
            }

            _sampleSize = _originalSampleSize = sampleSize;
            _maxSamplesToKeepPerSecond = maxSamplesToKeepPerSecond;
        }

        /// <summary>
        /// Start the data rate calculation. Must be called prior to calling <see cref="Add"/>.
        /// </summary>
        /// <param name="startTimestamp">Start timestamp. If null, the current time will be used.</param>
        public void Start(DateTimeOffset? startTimestamp = null)
        {
            lock (_locker)
            {
                _startTimestamp = startTimestamp ?? DateTimeOffset.UtcNow;

                _sampleSize = _originalSampleSize;
                _dataCount = 0;
                _dataPerSecond = null;

                // store all data to start with. we'll compute a store/drop rate after a data sample has been taken.
                _samplingModulus = 1;
                _samplingModulusMatchAction = SamplingAction.Keep;
            }
        }

        /// <summary>
        /// Add the specified datum to the data rate calculation. You must call <see cref="Start"/> before calling this method.
        /// </summary>
        /// <returns><see cref="SamplingAction"/> indicating whether the <see cref="Datum"/> should be kept (<see cref="SamplingAction.Keep"/>)
        /// or dropped (<see cref="SamplingAction.Drop"/>) to meet the <see cref="_maxSamplesToKeepPerSecond"/> requirement.</returns>
        /// <param name="datum">Datum to add.</param>
        public SamplingAction Add(Datum datum)
        {
            lock (_locker)
            {
                if (_startTimestamp == null)
                {
                    throw SensusException.Report("Data rate calculator has not been started.");
                }

                SamplingAction samplingAction = SamplingAction.Drop;

                if (datum != null)
                {
                    _dataCount++;

                    double maxSamplesToKeepPerSecond = _maxSamplesToKeepPerSecond.GetValueOrDefault(double.MaxValue);

                    // check whether the current datum should be kept as part of sampling. if a negligible data
                    // rate has been specified (e.g., 0 or something close to it), then we will never keep it.
                    if (maxSamplesToKeepPerSecond > DATA_RATE_EPSILON)
                    {
                        bool isModulusMatch = (_dataCount % _samplingModulus) == 0;
                        if ((isModulusMatch && _samplingModulusMatchAction == SamplingAction.Keep) ||
                            (!isModulusMatch && _samplingModulusMatchAction == SamplingAction.Drop))
                        {
                            samplingAction = SamplingAction.Keep;
                        }
                    }

                    // update data per second and sampling parameters for each new sample
                    if (_dataCount >= _sampleSize)
                    {
                        // recalculate data per second
                        _dataPerSecond = _dataCount / (datum.Timestamp - _startTimestamp.Value).TotalSeconds;

                        #region recalculate the sampling modulus/action for the new sampling rate
                        // in theory, the following code should work fine if a data rate of 0. however, the sampling
                        // modulus would then be infinity, and we cannot represent this with an integer. so check for
                        // a data rate of 0 and don't recalculate.
                        if (maxSamplesToKeepPerSecond > DATA_RATE_EPSILON)
                        {
                            // if no data rate is specified, maxDataStoresPerSecond will be double.MaxValue, making
                            // overage always negative and keeping all data.
                            double overagePerSecond = _dataPerSecond.Value - maxSamplesToKeepPerSecond;

                            // if we're not over the limit then keep all samples
                            if (overagePerSecond <= 0)
                            {
                                _samplingModulus = 1;
                                _samplingModulusMatchAction = SamplingAction.Keep;
                            }
                            // otherwise calculate a modulus that will get as close as possible to the desired rate given the empirical rate
                            else
                            {
                                double samplingModulusMatchRate = overagePerSecond / _dataPerSecond.Value;
                                _samplingModulusMatchAction = SamplingAction.Drop;

                                if (samplingModulusMatchRate > 0.5)
                                {
                                    samplingModulusMatchRate = 1 - samplingModulusMatchRate;
                                    _samplingModulusMatchAction = SamplingAction.Keep;
                                }

                                if (_samplingModulusMatchAction == SamplingAction.Keep)
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
                        }
                        #endregion

                        // the sample size must be at least as large as the sampling modulus, in order to hit the modulus
                        // at some point in the future. if the sampling modulus is smaller than the original, then use 
                        // the original as requested.
                        _sampleSize = Math.Max(_originalSampleSize, _samplingModulus);

                        // start a new sample
                        _dataCount = 0;
                        _startTimestamp = datum.Timestamp;
                    }
                }

                return samplingAction;
            }
        }

        public double? GetDataPerSecond()
        {
            lock (_locker)
            {
                return _dataPerSecond.HasValue ? _dataPerSecond.Value : default(double?);
            }
        }
    }
}
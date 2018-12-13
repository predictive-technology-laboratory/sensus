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
        private DateTimeOffset? _sampleStartTimestamp;
        private long _sampleDataCount;
        private DateTimeOffset? _mostRecentTimestamp;
        private double? _dataPerSecond;
        private long _samplingModulus;
        private SamplingAction _samplingModulusMatchAction;

        private readonly object _locker = new object();

        public long SampleSize
        {
            get { return _sampleSize; }
        }

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
                _sampleStartTimestamp = startTimestamp ?? DateTimeOffset.UtcNow;

                _sampleSize = _originalSampleSize;
                _sampleDataCount = 0;
                _mostRecentTimestamp = null;
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
                if (_sampleStartTimestamp == null)
                {
                    throw SensusException.Report("Data rate calculator has not been started.");
                }

                if (datum == null)
                {
                    return SamplingAction.Drop;
                }
                // data may arrive out of order. if the current datum precedes the sample start, we don't 
                // really know what to do with it because the sampling modulus and match action apply to the 
                // current sample and not any previous samples. for the sake of safety (to not lose data), 
                // just keep the datum. don't update the sample data count or timestamp, as the datum does
                // not apply to the current sample.
                else if (datum.Timestamp < _sampleStartTimestamp)
                {
                    return SamplingAction.Keep;
                }

                // the datum applies to the current sample. update the count.
                _sampleDataCount++;

                // if no maximum is specified, use the maximum possible.
                double maxSamplesToKeepPerSecond = _maxSamplesToKeepPerSecond.GetValueOrDefault(double.MaxValue);

                // check whether the current datum should be kept as part of sampling. if a negligible data
                // rate has been specified (e.g., 0 or something close to it), then we will never keep it.
                // if no sampling rate has been specified, then we'll be using a match modulus of 1 (always
                // match) and a match action of keep -- that is, we'll keep all data.
                SamplingAction samplingAction = SamplingAction.Drop;
                if (maxSamplesToKeepPerSecond > DATA_RATE_EPSILON)
                {
                    bool isModulusMatch = (_sampleDataCount % _samplingModulus) == 0;

                    if ((isModulusMatch && _samplingModulusMatchAction == SamplingAction.Keep) ||
                        (!isModulusMatch && _samplingModulusMatchAction == SamplingAction.Drop))
                    {
                        samplingAction = SamplingAction.Keep;
                    }
                }

                // update the most recent timestamp (samples might come out of order). we use this to
                // calculate the data rate.
                if (_mostRecentTimestamp == null)
                {
                    _mostRecentTimestamp = datum.Timestamp;
                }
                else if (datum.Timestamp > _mostRecentTimestamp.Value)
                {
                    _mostRecentTimestamp = datum.Timestamp;
                }

                // the sample is complete. update data per second and sampling parameters.
                if (_sampleDataCount >= _sampleSize)
                {
                    // recalculate data per second based on current count and most recent timestamp
                    _dataPerSecond = _sampleDataCount / (_mostRecentTimestamp.Value - _sampleStartTimestamp.Value).TotalSeconds;

                    #region recalculate the sampling modulus/action for the new sampling rate
                    // in theory, the following code should work fine if a data rate of 0. however, the sampling
                    // modulus would then be infinity, and we cannot represent this with an integer. so check for
                    // a data rate of 0 and don't recalculate.
                    if (maxSamplesToKeepPerSecond > DATA_RATE_EPSILON)
                    {
                        // if no data rate is specified, maxDataStoresPerSecond will be double.MaxValue, making
                        // overage always negative and keeping all data.
                        double overagePerSecond = _dataPerSecond.Value - maxSamplesToKeepPerSecond;

                        // if we're not over the limit then keep all future data until the next sample is complete.
                        if (overagePerSecond <= 0)
                        {
                            _samplingModulus = 1;
                            _samplingModulusMatchAction = SamplingAction.Keep;
                        }
                        // we've seen cases where the data and start timestamps are identical, resulting in an infinite data per second.
                        // the creates a sampling modulus of zero below and subsequent divide-by-zero errors above. drop all data until 
                        // a sample with more reasonable data timestamps comes in.
                        else if (double.IsInfinity(overagePerSecond))
                        {
                            SensusServiceHelper.Get().Logger.Log("Data and start timestamps are equal. Dropping all data.", LoggingLevel.Normal, GetType());
                            _samplingModulus = 1;
                            _samplingModulusMatchAction = SamplingAction.Drop;
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
                                _samplingModulus = (long)Math.Floor(1 / samplingModulusMatchRate);
                            }
                            else
                            {
                                // round the (drop) modulus up to oversample -- more is better, right?
                                _samplingModulus = (long)Math.Ceiling(1 / samplingModulusMatchRate);
                            }
                        }
                    }
                    #endregion

                    // the sample size must be at least as large as the sampling modulus, in order to hit the modulus
                    // at some point in the future. if the sampling modulus is smaller than the original, then use 
                    // the original as requested.
                    _sampleSize = Math.Max(_originalSampleSize, _samplingModulus);

                    // start a new sample from the most recent timestamp
                    _sampleDataCount = 0;
                    _sampleStartTimestamp = _mostRecentTimestamp.Value;
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
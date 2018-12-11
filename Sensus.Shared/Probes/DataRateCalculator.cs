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
                _startTimestamp = startTimestamp ?? DateTimeOffset.UtcNow;

                _sampleSize = _originalSampleSize;
                _dataCount = 0;
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
                if (_startTimestamp == null)
                {
                    throw SensusException.Report("Data rate calculator has not been started.");
                }

                SamplingAction samplingAction = SamplingAction.Drop;

                if (datum != null && datum.Timestamp >= _startTimestamp.Value)
                {
                    _dataCount++;

                    // update the most recent timestamp (samples might come out of order)
                    if (_mostRecentTimestamp == null)
                    {
                        _mostRecentTimestamp = datum.Timestamp;
                    }
                    else if (datum.Timestamp > _mostRecentTimestamp.Value)
                    {
                        _mostRecentTimestamp = datum.Timestamp;
                    }

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
                        // recalculate data per second based on current count and most recent timestamp
                        _dataPerSecond = _dataCount / (_mostRecentTimestamp.Value - _startTimestamp.Value).TotalSeconds;

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

                        // start a new sample
                        _dataCount = 0;
                        _startTimestamp = _mostRecentTimestamp.Value;
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

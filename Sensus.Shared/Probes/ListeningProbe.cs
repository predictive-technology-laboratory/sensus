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

using Sensus.UI.UiProperties;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

#if __ANDROID__
using Sensus.Android;
#endif

namespace Sensus.Probes
{
    /// <summary>
    /// Listening Probes are triggered by a change in state within the underlying device. For example, when an accelerometer reading is emitted, the 
    /// <see cref="Movement.AccelerometerProbe"/> is provided with information about device movement in each direction. Listening Probes do not generate
    /// data unless such state changes occur. Whether or not data streams are discontinued when the app is backgrounded depends on the operating
    /// system (Android or iOS) as well as specific settings.
    /// 
    /// * Android:  If <see cref="KeepDeviceAwake"/> is enabled on Android, then the app will hold a wake lock thus keeping the device processor powered
    /// on and delivering readings to the app. If <see cref="KeepDeviceAwake"/> is disabled on Android, then the CPU will be allowed to sleep typically
    /// discontinuing the delivery of readings to the app.
    /// 
    /// * iOS:  The <see cref="KeepDeviceAwake"/> setting has no effect on iOS, and all readings will be discontinued when the app is put into the
    /// background. See <see cref="PollingProbe"/> for more information about background considerations.
    /// </summary>
    public abstract class ListeningProbe : Probe
    {
        private double? _maxDataStoresPerSecond;
        private bool _keepDeviceAwake;
        private bool _deviceAwake;

        /// <summary>
        /// The maximum number of readings that may be stored in one second.
        /// </summary>
        /// <value>Maximum data stores per second.</value>
        [EntryDoubleUiProperty("Max Data / Second:", true, int.MaxValue, false)]
        public override double? MaxDataStoresPerSecond
        {
            get { return _maxDataStoresPerSecond; }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                _maxDataStoresPerSecond = value;
            }
        }

        [JsonIgnore]
        public TimeSpan? MinDataStoreDelay
        {
            get
            {
                double maxDataStoresPerSecond = _maxDataStoresPerSecond.GetValueOrDefault(-1);

                // 0 (or negligible) data per second:  maximum delay
                if (Math.Abs(maxDataStoresPerSecond) < DataRateCalculator.DATA_RATE_EPSILON)
                {
                    return TimeSpan.MaxValue;
                }
                // non-negligible data per second:  usual calculation
                else if (maxDataStoresPerSecond > 0)
                {
                    return TimeSpan.FromSeconds(1 / maxDataStoresPerSecond);
                }
                // unrestricted data per second:  no delay specified
                else
                {
                    return default(TimeSpan?);
                }
            }
        }

        /// <summary>
        /// Whether or not to keep the device awake while listening for readings while Sensus is backgrounded. If enabled, readings 
        /// will be delivered to Sensus in the background; however, more power will be consumed because the processor will not be allowed to sleep. If disabled,
        /// readings will be paused when Sensus is backgrounded. This will conserve power because the processor will be allowed to sleep, but readings will be 
        /// delayed and possibly dropped entirely. When the device wakes up, some readings that were cached while asleep may be delivered in bulk to Sensus. 
        /// This bulk delivery may not include all readings, and the readings delivered in bulk will have their <see cref="Datum.Timestamp"/> fields set to the
        /// time of bulk delivery rather than the time the reading originated. Even a single listening probe with this setting turned on will be sufficient to 
        /// keep the processor awake and delivering readings to all listening probes in all protocols within Sensus.
        /// </summary>
        /// <value><c>true</c> to keep device awake; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Keep Device Awake:", true, int.MaxValue - 1)]
        public bool KeepDeviceAwake
        {
            get
            {
                return _keepDeviceAwake;
            }
            set
            {
                // warn the user about this setting if it's being changed
                if (value != _keepDeviceAwake && SensusServiceHelper.Get() != null)
                {
                    TimeSpan duration = TimeSpan.FromSeconds(6);

                    if (value && !string.IsNullOrWhiteSpace(DeviceAwakeWarning))
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        SensusServiceHelper.Get().FlashNotificationAsync(DeviceAwakeWarning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                    else if (!value && !string.IsNullOrWhiteSpace(DeviceAsleepWarning))
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        SensusServiceHelper.Get().FlashNotificationAsync(DeviceAsleepWarning);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }

                _keepDeviceAwake = value;
            }
        }

        [JsonIgnore]
        protected abstract bool DefaultKeepDeviceAwake { get; }

        [JsonIgnore]
        protected abstract string DeviceAwakeWarning { get; }

        [JsonIgnore]
        protected abstract string DeviceAsleepWarning { get; }

        protected override double RawParticipation
        {
            get
            {
#if __ANDROID__
                // compute participation using successful health test times of the probe
                long daySeconds = 60 * 60 * 24;
                long participationHorizonSeconds = Protocol.ParticipationHorizonDays * daySeconds;
                double fullParticipationHealthTests = participationHorizonSeconds / SensusServiceHelper.HEALTH_TEST_DELAY.TotalSeconds;

                // lock collection because it might be concurrently modified by the test health method running in another thread.
                lock (SuccessfulHealthTestTimes)
                {
                    return SuccessfulHealthTestTimes.Count(healthTestTime => healthTestTime >= Protocol.ParticipationHorizon) / fullParticipationHealthTests;
                }
#elif __IOS__
                // on ios, we cannot rely on the health test times to tell us how long the probe has been running. this is
                // because, unlike in android, ios does not let local notifications return to the app when the app is in the 
                // background. instead, the ios user must open a notification or directly open the app in order for the health 
                // test to run. so the best we can do is keep track of when the probe was started and stopped and compute participation 
                // based on how much of the participation horizon has been covered by the probe. it is possible that the probe 
                // is in this running state but is somehow faulty and failing the health tests. thus, the approach is not 
                // perfect, but it's the best we can do on ios.
                double runningSeconds;

                lock (StartStopTimes)
                {
                    if (StartStopTimes.Count == 0)
                        return 0;

                    runningSeconds = StartStopTimes.Select((startStopTime, index) =>
                    {
                        DateTime? startTime = null;
                        DateTime? stopTime = null;

                        // if this is the final element and it's a start time, then the probe is currently running and we should calculate 
                        // how much time has elapsed since the probe was started.
                        if (index == StartStopTimes.Count - 1 && startStopTime.Item1)
                        {
                            // if the current start time came before the participation horizon, use the horizon as the start time.
                            if (startStopTime.Item2 < Protocol.ParticipationHorizon)
                            {
                                startTime = Protocol.ParticipationHorizon;
                            }
                            else
                            {
                                startTime = startStopTime.Item2;
                            }

                            // the probe is currently running, so use the current time as the stop time.
                            stopTime = DateTime.Now;
                        }
                        // otherwise, we only need to consider stop times after the participation horizon.
                        else if (!startStopTime.Item1 && startStopTime.Item2 > Protocol.ParticipationHorizon)
                        {
                            stopTime = startStopTime.Item2;

                            // if the previous element is a start time, use it.
                            if (index > 0 && StartStopTimes[index - 1].Item1)
                            {
                                startTime = StartStopTimes[index - 1].Item2;
                            }

                            // if we don't have a previous element that's a start time, or we do but the start time was before the participation horizon, then 
                            // use the participation horizon as the start time.
                            if (startTime == null || startTime.Value < Protocol.ParticipationHorizon)
                            {
                                startTime = Protocol.ParticipationHorizon;
                            }
                        }

                        // if we've got a start and stop time, return the total number of seconds covered.
                        if (startTime != null && stopTime != null)
                        {
                            return (stopTime.Value - startTime.Value).TotalSeconds;
                        }
                        else
                        {
                            return 0;
                        }

                    }).Sum();
                }

                double participationHorizonSeconds = TimeSpan.FromDays(Protocol.ParticipationHorizonDays).TotalSeconds;

                return (float)(runningSeconds / participationHorizonSeconds);
#elif LOCAL_TESTS
                return 0;
#else
#warning "Unrecognized platform"
                return 0;
#endif
            }
        }

        /// <summary>
        /// Gets the size of the data rate sample. Uses 10 times the <see cref="MaxDataStoresPerSecond"/> if one is specified, so 
        /// that the data rate and sampling parameters will be recalculated every 10 seconds when at maximum throughput. If no
        /// <see cref="MaxDataStoresPerSecond"/> is specified (no rate limit), then a data rate sample size of 10 will be used.
        /// </summary>
        /// <value>The size of the data rate sample.</value>
        protected override long DataRateSampleSize => _maxDataStoresPerSecond.HasValue ? (long)_maxDataStoresPerSecond.Value * 10 : 10;

        public override string CollectionDescription
        {
            get
            {
                return DisplayName + ":  " + (_maxDataStoresPerSecond.HasValue ? _maxDataStoresPerSecond.Value + " / sec." : "When it changes.");
            }
        }

        protected ListeningProbe()
        {
            _maxDataStoresPerSecond = null;  // no data rate limit by default
            _keepDeviceAwake = DefaultKeepDeviceAwake;
            _deviceAwake = false;
        }

        protected sealed override async Task ProtectedStartAsync()
        {
            // only keep device awake if we're not already running.
            if (!Running && _keepDeviceAwake)
            {
                await SensusServiceHelper.Get().KeepDeviceAwakeAsync();
                _deviceAwake = true;
            }

            await base.ProtectedStartAsync();

            await StartListeningAsync();
        }

        protected abstract Task StartListeningAsync();

        public sealed override async Task StopAsync()
        {
            await base.StopAsync();

            await StopListeningAsync();

            if (_deviceAwake)
            {
                await SensusServiceHelper.Get().LetDeviceSleepAsync();
                _deviceAwake = false;
            }
        }

        protected abstract Task StopListeningAsync();

        public override async Task ResetAsync()
        {
            await base.ResetAsync();
            _deviceAwake = false;
        }
    }
}

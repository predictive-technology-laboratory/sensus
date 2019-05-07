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
    /// <see cref="Movement.AccelerometerProbe"/> is provided with information about device movement in each direction. <see cref="ListeningProbe"/>s
    /// do not generate data unless such state changes occur (however, in the particular case of the <see cref="Movement.AccelerometerProbe"/>, the 
    /// hardware accelerometer chip will continuously be registering movement). Whether or not data streams are discontinued when the app is backgrounded 
    /// depends on the operating system (Android or iOS) as well as the setting of <see cref="KeepDeviceAwake"/>.
    /// </summary>
    public abstract class ListeningProbe : Probe, IListeningProbe
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
        /// Whether or not to keep the device awake and listening for readings, regardless of whether Sensus is backgrounded and the device
        /// is locked and idle. If enabled, then readings will be delivered to Sensus regardless of these states; however, more power will 
        /// be consumed because the processor will not be allowed to sleep. If disabled, then the effect depends on the operating system 
        /// and state of the device:
        /// 
        /// Android:  If disabled, then readings will be paused when the device enters the sleeping state (i.e., when it is locked and
        /// inactive). When the device wakes up (i.e., is unlocked or being actively used), then some readings that were cached while 
        /// asleep may be delivered in bulk to Sensus. This bulk delivery may not include all readings, and the readings delivered in 
        /// bulk will have their <see cref="Datum.Timestamp"/> fields set to the time of bulk delivery rather than the time the reading 
        /// originated.
        /// 
        /// iOS:  If disabled, then readings will be paused when the app enters the background state (e.g., by hitting the home button), 
        /// regardless of whether the phone is unlocked and actively being used. The only way to resume readings in this case is for the 
        /// user to bring the app to the foreground.
        /// 
        /// In any case above, if readings are paused then power will be conserved. If readings are flowing, then the CPU will be active 
        /// and will likely consume significant battery power. Furthermore, even a single <see cref="ListeningProbe"/> with this setting 
        /// enabled will be sufficient to keep readings flowing for all <see cref="ListeningProbe"/>s in all <see cref="Protocol"/>s 
        /// within Sensus. It is not possible to enable this setting for just one of many enabled <see cref="ListeningProbe"/>s.
        /// 
        /// Lastly, there are a few exceptions to the above as regards iOS. Certain <see cref="ListeningProbe"/>s will force the the CPU
        /// to remain awake and all readings to remain flowing on iOS. Currently these are the iOS implementation of
        /// <see cref="Probes.Location.CompassProbe"/> as well as <see cref="Probes.Location.ListeningLocationProbe"/>, 
        /// <see cref="Probes.Location.ListeningPointsOfInterestProximityProbe"/>, and <see cref="Probes.Movement.ListeningSpeedProbe"/>.
        /// If any one of these is enabled on iOS, then readings will continue to flow for all <see cref="ListeningProbe"/>s regardless
        /// of the value for <see cref="KeepDeviceAwake"/>.
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

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Sensus.Probes.ListeningProbe"/>, in its current
        /// configuration, will have a significant negative impact on battery. This can be the case if, e.g., 
        /// the probe has enabled <see cref="KeepDeviceAwake"/>, or if the probe depends on hardware/software 
        /// that is inherently battery hungry (e.g., <see cref="Location.ListeningLocationProbe"/> and the iOS
        /// version of the <see cref="Location.CompassProbe"/>, which depends on the GPS subsystem).
        /// </summary>
        /// <value><c>true</c> if has significant negative impact on battery; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        protected virtual bool WillHaveSignificantNegativeImpactOnBattery
        {
            get { return _keepDeviceAwake; }
        }

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
                return DisplayName + ":  " + (_maxDataStoresPerSecond.HasValue ? _maxDataStoresPerSecond.Value + " / sec." : "When it changes.") +
                                             (WillHaveSignificantNegativeImpactOnBattery ? " This sensor will have a significant negative impact on battery life." : "");
            }
        }

        protected ListeningProbe()
        {
            _maxDataStoresPerSecond = null;  // no data rate limit by default
            _keepDeviceAwake = DefaultKeepDeviceAwake;
            _deviceAwake = false;
        }

        protected override async Task ProtectedStartAsync()
        {
            await base.ProtectedStartAsync();

            if (_keepDeviceAwake)
            {
                await SensusServiceHelper.Get().KeepDeviceAwakeAsync();
                _deviceAwake = true;
            }

            await StartListeningAsync();
        }

        protected virtual Task StartListeningAsync()
        {
            SensusServiceHelper.Get().Logger.Log("Starting listening...", LoggingLevel.Normal, GetType());
            return Task.CompletedTask;
        }

        protected override async Task ProtectedStopAsync()
        {
            await base.ProtectedStopAsync();

            await StopListeningAsync();

            if (_deviceAwake)
            {
                await SensusServiceHelper.Get().LetDeviceSleepAsync();
                _deviceAwake = false;
            }
        }

        protected virtual Task StopListeningAsync()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping listening...", LoggingLevel.Normal, GetType());
            return Task.CompletedTask;
        }

        public override async Task ResetAsync()
        {
            await base.ResetAsync();
            _deviceAwake = false;
        }
    }
}

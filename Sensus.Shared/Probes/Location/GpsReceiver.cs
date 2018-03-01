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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// A GPS receiver. Implemented as a singleton.
    /// </summary>
    public class GpsReceiver
    {
        #region static members

        public static readonly GpsReceiver SINGLETON = new GpsReceiver();

        public static GpsReceiver Get()
        {
            return SINGLETON;
        }

        #endregion

        private event EventHandler<PositionEventArgs> PositionChanged;

        private IGeolocator _locator;
        private bool _readingIsComing;
        private ManualResetEvent _readingWait;
        private Position _reading;
        private int _readingTimeoutMS;
        private List<Tuple<EventHandler<PositionEventArgs>, bool>> _listenerHeadings;

        private readonly object _locker = new object();

        public IGeolocator Locator
        {
            get { return _locator; }
        }

        private bool ListeningForChanges
        {
            get { return PositionChanged != null; }
        }

        public int MinimumDistanceThreshold
        {
            // because GPS is only so accurate, successive readings can fire the trigger even if one is not moving -- if the threshold is too small. theoretically
            // the minimum value of the threshold should be equal to the desired accuracy; however, the desired accuracy is just a request. use 2x the desired
            // accuracy just to be sure.
            get { return (int)(2 * _locator.DesiredAccuracy); }
        }

        private GpsReceiver()
        {
            _readingIsComing = false;
            _readingWait = new ManualResetEvent(false);
            _reading = null;
            _readingTimeoutMS = 120000;
            _listenerHeadings = new List<Tuple<EventHandler<PositionEventArgs>, bool>>();
            _locator = CrossGeolocator.Current;
            _locator.DesiredAccuracy = SensusServiceHelper.Get().GpsDesiredAccuracyMeters;
            _locator.PositionChanged += (o, e) =>
            {
                SensusServiceHelper.Get().Logger.Log("GPS position has changed.", LoggingLevel.Verbose, GetType());

                PositionChanged?.Invoke(o, e);
            };
        }

        public async void AddListener(EventHandler<PositionEventArgs> listener, bool includeHeading)
        {
            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                throw new Exception("Could not access GPS.");
            }

            // if we're already listening, stop listening first so that the locator can be configured with
            // the most recent listening settings below.
            if (ListeningForChanges)
            {
                await _locator.StopListeningAsync();
            }

            // add new listener
            PositionChanged += listener;
            _listenerHeadings.Add(new Tuple<EventHandler<PositionEventArgs>, bool>(listener, includeHeading));

            _locator.DesiredAccuracy = SensusServiceHelper.Get().GpsDesiredAccuracyMeters;

            await _locator.StartListeningAsync(TimeSpan.FromMilliseconds(SensusServiceHelper.Get().GpsMinTimeDelayMS), SensusServiceHelper.Get().GpsMinDistanceDelayMeters, _listenerHeadings.Any(t => t.Item2), GetListenerSettings());

            SensusServiceHelper.Get().Logger.Log("GPS receiver is now listening for changes.", LoggingLevel.Normal, GetType());
        }

        public async void RemoveListener(EventHandler<PositionEventArgs> listener)
        {
            if (ListeningForChanges)
            {
                await _locator.StopListeningAsync();
            }

            PositionChanged -= listener;

            _listenerHeadings.RemoveAll(t => t.Item1 == listener);

            if (ListeningForChanges)
            {
                await _locator.StartListeningAsync(TimeSpan.FromMilliseconds(SensusServiceHelper.Get().GpsMinTimeDelayMS), SensusServiceHelper.Get().GpsMinDistanceDelayMeters, _listenerHeadings.Any(t => t.Item2), GetListenerSettings());
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("All listeners removed from GPS receiver. Stopped listening.", LoggingLevel.Normal, GetType());
            }
        }

        private ListenerSettings GetListenerSettings()
        {
            ListenerSettings settings = null;

#if __IOS__
            float gpsDeferralDistanceMeters = SensusServiceHelper.Get().GpsDeferralDistanceMeters;
            float gpsDeferralTimeMinutes = SensusServiceHelper.Get().GpsDeferralTimeMinutes;

            settings = new ListenerSettings
            {
                AllowBackgroundUpdates = true,
                PauseLocationUpdatesAutomatically = SensusServiceHelper.Get().GpsPauseLocationUpdatesAutomatically,
                ActivityType = SensusServiceHelper.Get().GpsActivityType,
                ListenForSignificantChanges = SensusServiceHelper.Get().GpsListenForSignificantChanges,
                DeferLocationUpdates = SensusServiceHelper.Get().GpsDeferLocationUpdates,
                DeferralDistanceMeters = gpsDeferralDistanceMeters < 0 ? default(double?) : gpsDeferralDistanceMeters,
                DeferralTime = gpsDeferralTimeMinutes < 0 ? default(TimeSpan?) : TimeSpan.FromMinutes(gpsDeferralTimeMinutes)
            };
#endif

            return settings;
        }

        /// <summary>
        /// Gets a GPS reading. Will block the current thread while waiting for a GPS reading. Should not
        /// be called from the main / UI thread, since GPS runs on main thread (will deadlock).
        /// </summary>
        /// <returns>The reading.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="checkAndObtainPermission">Whether or not to check for and obtain permission for the reading.</param>
        public Position GetReading(CancellationToken cancellationToken, bool checkAndObtainPermission)
        {
            return GetReading(0, cancellationToken, checkAndObtainPermission);
        }

        /// <summary>
        /// Gets a GPS reading, reusing an old one if it isn't too old. Will block the current thread while waiting for a GPS reading. Should not
        /// be called from the main / UI thread, since GPS runs on main thread (will deadlock).
        /// </summary>
        /// <returns>The reading.</returns>
        /// <param name="maxReadingAgeForReuseMS">Maximum age of old reading to reuse (milliseconds).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="checkAndObtainPermission">Whether or not to check for and obtain permission for the reading. Note that, on Android, this
        /// may result in bringing the Sensus UI to the foreground. If you do not wish this to happen, then obtain the user's permission prior to
        /// calling this method.</param>
        public Position GetReading(int maxReadingAgeForReuseMS, CancellationToken cancellationToken, bool checkAndObtainPermission)
        {
            lock (_locker)
            {
                if (checkAndObtainPermission && SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
                {
                    return null;
                }

                // reuse existing reading if it isn't too old
                if (_reading != null && maxReadingAgeForReuseMS > 0)
                {
                    double readingAgeMS = (DateTimeOffset.UtcNow - _reading.Timestamp).TotalMilliseconds;
                    if (readingAgeMS <= maxReadingAgeForReuseMS)
                    {
                        SensusServiceHelper.Get().Logger.Log("Reusing previous GPS reading, which is " + readingAgeMS + "ms old (maximum = " + maxReadingAgeForReuseMS + "ms).", LoggingLevel.Verbose, GetType());
                        return _reading;
                    }
                }

                if (_readingIsComing)
                {
                    SensusServiceHelper.Get().Logger.Log("A GPS reading is coming. Will wait for it.", LoggingLevel.Debug, GetType());
                }
                else
                {
                    _readingIsComing = true;  // tell any subsequent, concurrent callers that we're taking a reading
                    _readingWait.Reset();  // make them wait

                    Task.Run(async () =>
                    {
                        try
                        {
                            SensusServiceHelper.Get().Logger.Log("Taking GPS reading.", LoggingLevel.Debug, GetType());

                            DateTimeOffset readingStart = DateTimeOffset.UtcNow;
                            _locator.DesiredAccuracy = SensusServiceHelper.Get().GpsDesiredAccuracyMeters;
                            Position newReading = await _locator.GetPositionAsync(TimeSpan.FromMilliseconds(_readingTimeoutMS), cancellationToken);
                            DateTimeOffset readingEnd = DateTimeOffset.UtcNow;

                            if (newReading != null)
                            {
                                // create copy of new position to keep return references separate, since the same Position object is returned multiple times when a change listener is attached.
                                _reading = new Position(newReading);

                                SensusServiceHelper.Get().Logger.Log("GPS reading obtained in " + (readingEnd - readingStart).TotalSeconds + " seconds.", LoggingLevel.Verbose, GetType());
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("GPS reading failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                            _reading = null;
                        }

                        _readingWait.Set();  // tell anyone waiting on the shared reading that it is ready
                        _readingIsComing = false;  // direct any future calls to this method to get their own reading
                    });
                }
            }

            _readingWait.WaitOne(_readingTimeoutMS);

            if (_reading == null)
            {
                SensusServiceHelper.Get().Logger.Log("GPS reading is null.", LoggingLevel.Normal, GetType());
            }

            return _reading;
        }
    }
}
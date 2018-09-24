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
        private TimeSpan _readingTimeout;
        private List<Tuple<EventHandler<PositionEventArgs>, bool>> _listenerHeadings;

        private readonly object _readingLocker = new object();
        private TaskCompletionSource<Position> _readingCompletionSource;
        private bool _takingReading;

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
            _readingTimeout = TimeSpan.FromMinutes(2);
            _listenerHeadings = new List<Tuple<EventHandler<PositionEventArgs>, bool>>();
            _locator = CrossGeolocator.Current;
            _locator.DesiredAccuracy = SensusServiceHelper.Get().GpsDesiredAccuracyMeters;
            _locator.PositionChanged += (o, e) =>
            {
                SensusServiceHelper.Get().Logger.Log("GPS position has changed.", LoggingLevel.Verbose, GetType());

                PositionChanged?.Invoke(o, e);
            };
        }

        public async Task AddListenerAsync(EventHandler<PositionEventArgs> listener, bool includeHeading)
        {
            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
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

        public async Task RemoveListenerAsync(EventHandler<PositionEventArgs> listener)
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

        public async Task<Position> GetReadingAsync(CancellationToken cancellationToken, bool checkAndObtainPermission)
        {
            bool takeReading = false;

            lock (_readingLocker)
            {
                if (!_takingReading)
                {
                    _takingReading = true;
                    _readingCompletionSource = new TaskCompletionSource<Position>();
                    takeReading = true;
                }
            }

            Position reading = null;

            if (takeReading)
            {
                try
                {
                    if (checkAndObtainPermission && await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                    {
                        throw new Exception("Failed to obtain Location permission from user.");
                    }

                    SensusServiceHelper.Get().Logger.Log("Taking GPS reading.", LoggingLevel.Normal, GetType());

                    DateTimeOffset readingStart = DateTimeOffset.UtcNow;
                    reading = await _locator.GetPositionAsync(_readingTimeout, cancellationToken);
                    DateTimeOffset readingEnd = DateTimeOffset.UtcNow;

                    if (reading != null)
                    {
                        // create copy of new position to keep return references separate, since the same Position object is returned multiple times when a change listener is attached.
                        reading = new Position(reading);

                        SensusServiceHelper.Get().Logger.Log("GPS reading obtained in " + (readingEnd - readingStart).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("GPS reading failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                    reading = null;
                }
                finally
                {
                    _readingCompletionSource.SetResult(reading);
                    _takingReading = false;
                }
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Waiting for reading...", LoggingLevel.Normal, GetType());
                reading = await _readingCompletionSource.Task;
                SensusServiceHelper.Get().Logger.Log("..." + (reading == null ? " null " : "") + " reading arrived.", LoggingLevel.Normal, GetType());
            }

            return reading;
        }
    }
}
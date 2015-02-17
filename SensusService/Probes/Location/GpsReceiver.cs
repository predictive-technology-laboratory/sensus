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

using SensusService.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// A GPS receiver. Implemented as a singleton.
    /// </summary>
    public class GpsReceiver
    {
        public static GpsReceiver _singleton = new GpsReceiver();

        public static GpsReceiver Get()
        {
            return _singleton;
        }

        private event EventHandler<PositionEventArgs> PositionChanged;

        private Geolocator _locator;
        private int _desiredAccuracyMeters;
        private bool _sharedReadingIsComing;
        private ManualResetEvent _sharedReadingWait;
        private Position _sharedReading;
        private DateTimeOffset _sharedReadingTimestamp;
        private int _readingTimeoutMS;
        private int _minimumTimeHint;
        private int _minimumDistanceHint;

        private readonly object _locker = new object();

        public Geolocator Locator
        {
            get { return _locator; }
            set { _locator = value; }
        }

        public int DesiredAccuracyMeters
        {
            get { return _desiredAccuracyMeters; }
            set
            {
                _desiredAccuracyMeters = value;

                if (_locator != null)
                    _locator.DesiredAccuracy = value;
            }
        }

        public int MinimumTimeHint
        {
            get { return _minimumTimeHint; }
            set
            {
                if (value != _minimumTimeHint)
                {
                    _minimumTimeHint = value;

                    if (ListeningForChanges)
                    {
                        _locator.StopListening();
                        _locator.StartListening(_minimumTimeHint, _minimumDistanceHint, true);
                    }
                }
            }
        }

        public int MinimumDistanceHint
        {
            get { return _minimumDistanceHint; }
            set
            {
                if (value != _minimumDistanceHint)
                {
                    _minimumDistanceHint = value;

                    if (ListeningForChanges)
                    {
                        _locator.StopListening();
                        _locator.StartListening(_minimumTimeHint, _minimumDistanceHint, true);
                    }
                }
            }
        }

        public bool ListeningForChanges
        {
            get { return PositionChanged != null; }
        }

        private GpsReceiver()
        {
            _desiredAccuracyMeters = 10;
            _sharedReadingIsComing = false;
            _sharedReadingWait = new ManualResetEvent(false);
            _sharedReading = null;
            _sharedReadingTimestamp = DateTimeOffset.MinValue;
            _minimumTimeHint = 60000;
            _minimumDistanceHint = 100;
            _readingTimeoutMS = 120000;
        }

        public void AddListener(EventHandler<PositionEventArgs> listener)
        {
            lock (_locker)
            {
                if (_locator == null)
                    throw new SensusException("Locator has not yet been bound to a platform-specific implementation.");

                if (ListeningForChanges)
                    _locator.StopListening();

                PositionChanged += listener;

                _locator.StartListening(_minimumTimeHint, _minimumDistanceHint, true);

                SensusServiceHelper.Get().Logger.Log("GPS receiver is now listening for changes.", LoggingLevel.Normal);
            }
        }

        public void RemoveListener(EventHandler<PositionEventArgs> listener)
        {
            lock (_locker)
            {
                if (_locator == null)
                    throw new SensusException("Locator has not yet been bound to a platform-specific implementation.");

                if (ListeningForChanges)
                    _locator.StopListening();

                PositionChanged -= listener;

                if (ListeningForChanges)
                    _locator.StartListening(_minimumTimeHint, _minimumDistanceHint, true);
                else
                    SensusServiceHelper.Get().Logger.Log("All listeners removed from GPS receiver. Stopped listening.", LoggingLevel.Normal);
            }
        }

        public void ClearListeners()
        {
            lock (_locker)
            {
                _locator.StopListening();
                PositionChanged = null;

                SensusServiceHelper.Get().Logger.Log("All listeners removed from GPS receiver. Stopped listening.", LoggingLevel.Normal);
            }
        }

        public void Initialize(Geolocator locator)
        {
            _locator = locator;

            _locator.DesiredAccuracy = _desiredAccuracyMeters;

            _locator.PositionChanged += (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("GPS position has changed:  " + e.Position.Latitude + " " + e.Position.Longitude, LoggingLevel.Verbose);

                    if (PositionChanged != null)
                        PositionChanged(o, e);
                };

            _locator.PositionError += (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("Position error from GPS receiver:  " + e.Error, LoggingLevel.Normal);
                };
        }

        public Position GetReading(int maxSharedReadingAgeForReuseMS)
        {
            // reuse a previous reading if it isn't too old
            TimeSpan sharedReadingAge = DateTimeOffset.UtcNow - _sharedReadingTimestamp;
            if (sharedReadingAge.TotalMilliseconds < maxSharedReadingAgeForReuseMS)
            {
                SensusServiceHelper.Get().Logger.Log("Reusing previous GPS reading, which is " + sharedReadingAge.TotalMilliseconds + " MS old (maximum=" + maxSharedReadingAgeForReuseMS + ").", LoggingLevel.Verbose);

                return _sharedReading;
            }

            if (!_sharedReadingIsComing)  // is someone else currently taking a reading? if so, wait for that instead.
            {
                _sharedReadingIsComing = true;  // tell any subsequent, concurrent callers that we're taking a reading
                _sharedReadingWait.Reset();  // make them wait
                new Thread(async () =>
                    {
                        try
                        {
                            SensusServiceHelper.Get().Logger.Log("Taking shared reading.", LoggingLevel.Debug);

                            DateTimeOffset start = DateTimeOffset.UtcNow;
                            _sharedReading = await _locator.GetPositionAsync(timeout: _readingTimeoutMS);
                            DateTimeOffset end = _sharedReadingTimestamp = DateTimeOffset.UtcNow;

                            if (_sharedReading != null)
                                SensusServiceHelper.Get().Logger.Log("Shared reading obtained in " + (end - start).TotalSeconds + " seconds:  " + _sharedReading.Latitude + " " + _sharedReading.Longitude, LoggingLevel.Verbose);
                        }
                        catch (TaskCanceledException ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("GPS reading task canceled:  " + ex.Message, LoggingLevel.Normal);

                            _sharedReading = null;
                        }

                        _sharedReadingIsComing = false;  // direct any future calls to this method to get their own reading
                        _sharedReadingWait.Set();  // tell anyone waiting on the shared reading that it is ready

                    }).Start();
            }
            else
                SensusServiceHelper.Get().Logger.Log("A shared reading is coming. Will wait for it.", LoggingLevel.Debug);

            _sharedReadingWait.WaitOne(_readingTimeoutMS * 2);  // wait twice the locator timeout, just to be sure.

            Position reading = _sharedReading;

            if (reading == null)
                SensusServiceHelper.Get().Logger.Log("Shared reading is null.", LoggingLevel.Normal);

            return reading;
        }
    }
}

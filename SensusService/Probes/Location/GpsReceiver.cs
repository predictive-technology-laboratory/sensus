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
        #region static members
        public static GpsReceiver _singleton = new GpsReceiver();

        public static GpsReceiver Get()
        {
            return _singleton;
        }
        #endregion

        private event EventHandler<PositionEventArgs> PositionChanged;

        private Geolocator _locator;
        private int _desiredAccuracyMeters;
        private bool _readingIsComing;
        private ManualResetEvent _readingWait;
        private Position _reading;
        private DateTimeOffset _readingTimestamp;
        private int _readingTimeoutMS;
        private int _minimumTimeHintMS;

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
                    _locator.DesiredAccuracy = _desiredAccuracyMeters;
            }
        }

        public int MinimumTimeHintMS
        {
            get { return _minimumTimeHintMS; }
            set
            {
                if (value != _minimumTimeHintMS)
                {
                    _minimumTimeHintMS = value;

                    if (ListeningForChanges)
                    {
                        _locator.StopListening();
                        _locator.StartListening(_minimumTimeHintMS, _desiredAccuracyMeters, true);
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
            _readingIsComing = false;
            _readingWait = new ManualResetEvent(false);
            _reading = null;
            _readingTimestamp = DateTimeOffset.MinValue;
            _readingTimeoutMS = 120000;
            _minimumTimeHintMS = 10000;
        }

        public void Initialize(Geolocator locator)
        {
            _locator = locator;
            _locator.DesiredAccuracy = _desiredAccuracyMeters;

            _locator.PositionChanged += (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("GPS position has changed:  " + e.Position.Latitude + " " + e.Position.Longitude, LoggingLevel.Verbose, GetType());

                    if (PositionChanged != null)
                        PositionChanged(o, e);
                };

            _locator.PositionError += (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("Position error from GPS receiver:  " + e.Error, LoggingLevel.Normal, GetType());
                };
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

                _locator.StartListening(_minimumTimeHintMS, _desiredAccuracyMeters, true);

                SensusServiceHelper.Get().Logger.Log("GPS receiver is now listening for changes.", LoggingLevel.Normal, GetType());
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
                    _locator.StartListening(_minimumTimeHintMS, _desiredAccuracyMeters, true);
                else
                    SensusServiceHelper.Get().Logger.Log("All listeners removed from GPS receiver. Stopped listening.", LoggingLevel.Normal, GetType());
            }
        }

        public void ClearListeners()
        {
            lock (_locker)
            {
                _locator.StopListening();
                PositionChanged = null;

                SensusServiceHelper.Get().Logger.Log("All listeners removed from GPS receiver. Stopped listening.", LoggingLevel.Normal, GetType());
            }
        }           

        public Position GetReading()
        {
            return GetReading(0);
        }

        public Position GetReading(int maxReadingAgeForReuseMS)
        {
            // reuse a previous reading if it isn't too old
            TimeSpan readingAge = DateTimeOffset.UtcNow - _readingTimestamp;
            if (readingAge.TotalMilliseconds <= maxReadingAgeForReuseMS)
            {
                SensusServiceHelper.Get().Logger.Log("Reusing previous GPS reading, which is " + readingAge.TotalMilliseconds + " MS old (maximum=" + maxReadingAgeForReuseMS + ").", LoggingLevel.Verbose, GetType());
                return _reading;
            }

            lock (_locker)
                if (_readingIsComing)  // is someone else currently taking a reading? if so, wait for that instead.
                    SensusServiceHelper.Get().Logger.Log("A GPS reading is coming. Will wait for it.", LoggingLevel.Debug, GetType());
                else
                {
                    _readingIsComing = true;  // tell any subsequent, concurrent callers that we're taking a reading
                    _readingWait.Reset();  // make them wait

                    new Thread(async () =>
                        {
                            try
                            {
                                SensusServiceHelper.Get().Logger.Log("Taking GPS reading.", LoggingLevel.Debug, GetType());

                                DateTimeOffset readingStart = DateTimeOffset.UtcNow;
                                _reading = await _locator.GetPositionAsync(timeout: _readingTimeoutMS);
                                DateTimeOffset readingEnd = _readingTimestamp = DateTimeOffset.UtcNow;

                                if (_reading != null)
                                    SensusServiceHelper.Get().Logger.Log("GPS reading obtained in " + (readingEnd - readingStart).TotalSeconds + " seconds:  " + _reading.Latitude + " " + _reading.Longitude, LoggingLevel.Verbose, GetType());
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("GPS reading failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                                _reading = null;
                            }

                            _readingWait.Set();  // tell anyone waiting on the shared reading that it is ready
                            _readingIsComing = false;  // direct any future calls to this method to get their own reading

                        }).Start();
                }

            _readingWait.WaitOne(_readingTimeoutMS * 2);  // wait twice the locator timeout, just to be sure.

            if (_reading == null)
                SensusServiceHelper.Get().Logger.Log("GPS reading is null.", LoggingLevel.Normal, GetType());

            return _reading;
        }
    }
}
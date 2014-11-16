using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// A GPS receiver. Implemented as a singleton.
    /// </summary>
    public class GpsReceiver
    {
        public static GpsReceiver _singleton = new GpsReceiver();

        public static GpsReceiver Get()
        {
            if (_singleton == null)
                throw new InvalidOperationException("GpsReceiver has not yet been initialize.");

            return _singleton;
        }

        private event EventHandler<PositionEventArgs> PositionChanged;

        private Geolocator _locator;
        private int _desiredAccuracyMeters;
        private bool _sharedReadingIsComing;
        private ManualResetEvent _sharedReadingWaitHandle;
        private Position _sharedReading;
        private DateTime _sharedReadingTimestamp;
        private bool _listeningForChanges;
        private int _minimumTimeHint;
        private int _minimumDistanceHint;

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

                    if (_listeningForChanges)
                    {
                        StopListeningForChanges();
                        StartListeningForChanges();
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

                    if (_listeningForChanges)
                    {
                        StopListeningForChanges();
                        StartListeningForChanges();
                    }
                }
            }
        }

        private GpsReceiver()
        {
            _desiredAccuracyMeters = 50;
            _sharedReadingIsComing = false;
            _sharedReadingWaitHandle = new ManualResetEvent(false);
            _sharedReading = null;
            _sharedReadingTimestamp = DateTime.MinValue;
            _minimumTimeHint = 60000;
            _minimumDistanceHint = 100;
            _listeningForChanges = false;
        }

        public void AddListener(EventHandler<PositionEventArgs> listener)
        {
            lock (this)
                PositionChanged += listener;
        }

        public void RemoveListener(EventHandler<PositionEventArgs> listener)
        {
            lock (this)
            {
                PositionChanged -= listener;

                if (PositionChanged == null)
                {
                    if (Logger.Level >= LoggingLevel.Normal)
                        Logger.Log("All listeners removed from GPS receiver. Stopping listening.");

                    StopListeningForChanges();
                }
            }
        }

        public void ClearListeners()
        {
            lock (this) { PositionChanged = null; }
        }

        public void Initialize(Geolocator locator)
        {
            _locator = locator;

            // if we are being initialized by a platform-specific initializer, we will have access to a geolocator and so should set the desired accuracy.
            _locator.DesiredAccuracy = _desiredAccuracyMeters;

            _locator.PositionChanged += (o, e) =>
                {
                    if (Logger.Level >= LoggingLevel.Verbose)
                        Logger.Log("GPS position has changed:  " + e.Position.Latitude + " " + e.Position.Longitude);

                    if (PositionChanged != null)
                        PositionChanged(o, e);
                };

            _locator.PositionError += (o, e) =>
                {
                    Logger.Log("Position error from GPS receiver:  " + e.Error);
                };
        }

        public Position GetReading(int maxSharedReadingAgeForReuseMS, int timeout)
        {
            // reuse a previous reading if it isn't too old
            TimeSpan sharedReadingAge = DateTime.Now - _sharedReadingTimestamp;
            if (sharedReadingAge.TotalMilliseconds < maxSharedReadingAgeForReuseMS)
            {
                if (Logger.Level >= LoggingLevel.Verbose)
                    Logger.Log("Reusing previous GPS reading, which is " + sharedReadingAge.TotalMilliseconds + " MS old (maximum=" + maxSharedReadingAgeForReuseMS + ").");

                return _sharedReading;
            }

            if (!_sharedReadingIsComing)  // is someone else currently taking a reading? if so, wait for that instead.
            {
                _sharedReadingIsComing = true;  // tell any subsequent, concurrent callers that we're taking a reading
                _sharedReadingWaitHandle.Reset();  // make them wait
                Task readingTask = Task.Run(async () =>
                    {
                        try
                        {
                            if (Logger.Level >= LoggingLevel.Debug)
                                Logger.Log("Taking shared reading.");

                            DateTime start = DateTime.Now;
                            _sharedReading = await _locator.GetPositionAsync(timeout: timeout);
                            DateTime end = _sharedReadingTimestamp = DateTime.Now;

                            if (_sharedReading != null && Logger.Level >= LoggingLevel.Verbose)
                                Logger.Log("Shared reading obtained in " + (end - start).Milliseconds + " MS:  " + _sharedReading.Latitude + " " + _sharedReading.Longitude);
                        }
                        catch (TaskCanceledException ex)
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("GPS reading task canceled:  " + ex.Message + Environment.NewLine + ex.StackTrace);

                            _sharedReading = null;
                        }

                        _sharedReadingIsComing = false;  // direct any future calls to this method to get their own reading
                        _sharedReadingWaitHandle.Set();  // tell anyone waiting on the shared reading that it is ready
                    });
            }
            else if (Logger.Level >= LoggingLevel.Debug)
                Logger.Log("A shared reading is coming. Will wait for it.");

            _sharedReadingWaitHandle.WaitOne(timeout * 2);  // wait twice the locator timeout, just to be sure.

            Position reading = _sharedReading;

            if (reading == null)
            {
                if (Logger.Level >= LoggingLevel.Normal)
                    Logger.Log("Shared reading is null.");
            }

            return reading;
        }

        public void StartListeningForChanges()
        {
            if (_locator == null)
                throw new InvalidOperationException("Locator has not yet been bound to a platform-specific implementation.");

            if (_listeningForChanges)
                return;

            _listeningForChanges = true;
            _locator.StartListening(_minimumTimeHint, _minimumDistanceHint, true);
        }

        public void StopListeningForChanges()
        {
            if (PositionChanged != null)
                throw new InvalidOperationException("Cannot stop listening for position changes while there are still subscribers.");

            if (!_listeningForChanges || _locator == null)
                return;

            _locator.StopListening();
            _listeningForChanges = false;
        }
    }
}

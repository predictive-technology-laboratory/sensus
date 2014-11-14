using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// A GPS receiver.
    /// </summary>
    public class GpsReceiver
    {
        private static bool _sharedReadingIsComing = false;
        private static Position _sharedReading = null;
        private ManualResetEvent _sharedReadingWaitHandle = new ManualResetEvent(false);

        public event EventHandler<PositionEventArgs> PositionChanged;
        public event EventHandler<PositionErrorEventArgs> PositionError;

        private Geolocator _locator;
        private int _desiredAccuracyMeters;

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

                // if the receiver has already been initialized, simply update the value
                if (_locator != null)
                    _locator.DesiredAccuracy = value;
            }
        }

        public GpsReceiver()
        {
            _desiredAccuracyMeters = 50;
        }

        public ProbeState Initialize()
        {
            // the receiver is configured by platform-specific initializers at the time of protocol execution. prior to this, calls to Initialize won't have access to a Geolocator.
            if (_locator == null)
            {
                if (Logger.Level >= LoggingLevel.Normal)
                    Logger.Log("GPS receiver is not yet bound to a locator.");

                return ProbeState.Uninitialized;
            }
            else
            {
                if (Logger.Level >= LoggingLevel.Normal)
                    Logger.Log("GPS receiver is now bound to a locator.");

                // if we are being initialized by a platform-specific initializer, we will have access to a geolocator and so should set the desired accuracy.
                _locator.DesiredAccuracy = _desiredAccuracyMeters;

                _locator.PositionChanged += (o, e) =>
                    {
                        if (PositionChanged != null)
                            PositionChanged(o, e);
                    };

                _locator.PositionError += (o, e) =>
                    {
                        if (PositionError != null)
                            PositionError(o, e);
                    };

                return ProbeState.Initialized;
            }
        }

        public Position GetReading(int timeout)
        {
            if (!_sharedReadingIsComing)
            {
                _sharedReadingIsComing = true;
                _sharedReadingWaitHandle.Reset();
                Task readingTask = Task.Run(async () =>
                    {
                        try
                        {
                            if (Logger.Level >= LoggingLevel.Debug)
                                Logger.Log("Taking shared reading.");

                            DateTime start = DateTime.Now;
                            _sharedReading = await _locator.GetPositionAsync(timeout: timeout);
                            DateTime end = DateTime.Now;

                            if (_sharedReading != null && Logger.Level >= LoggingLevel.Verbose)
                                Logger.Log("Shared reading obtained in " + (end - start).Milliseconds + " MS:  " + _sharedReading.Latitude + " " + _sharedReading.Longitude);
                        }
                        catch (TaskCanceledException ex)
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("GPS reading task canceled:  " + ex.Message + Environment.NewLine + ex.StackTrace);

                            _sharedReading = null;
                        }

                        _sharedReadingIsComing = false;
                        _sharedReadingWaitHandle.Set();
                    });
            }
            else if (Logger.Level >= LoggingLevel.Debug)
                Logger.Log("A shared reading is coming. Will wait for it.");

            _sharedReadingWaitHandle.WaitOne();

            Position reading = _sharedReading;

            if (reading == null)
            {
                if (Logger.Level >= LoggingLevel.Normal)
                    Logger.Log("Shared reading is null.");
            }

            return reading;
        }

        /// <summary>
        /// Starts listening for changes in location.
        /// </summary>
        /// <param name="minimumTime">A hint for the minimum time in position updates in milliseconds.</param>
        /// <param name="minimumDistance">A hint for the minimum time in position updates in milliseconds.</param>
        /// <param name="includeHeading">Whether or not to also listen for heading.</param>
        public void StartListeningForChanges(int minimumTime, int minimumDistance, bool includeHeading)
        {
            _locator.StartListening(minimumTime, minimumDistance, includeHeading);
        }

        public void StopListeningForChanges()
        {
            _locator.StopListening();
        }
    }
}

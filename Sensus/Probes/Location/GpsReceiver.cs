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
        public event EventHandler<PositionEventArgs> PositionChanged;
        public event EventHandler<PositionErrorEventArgs> PositionError;

        private Geolocator _locator;
        private int _desiredAccuracyMeters;
        private Position _mostRecentReading;

        public Geolocator Locator
        {
            get { return _locator; }
            set { _locator = value; }
        }

        public int DesiredAccuracyMeters
        {
            get { return _desiredAccuracyMeters; }
            set { _desiredAccuracyMeters = value; }
        }

        public Position MostRecentReading
        {
            get { return _mostRecentReading; }
        }

        public GpsReceiver()
        {
            _desiredAccuracyMeters = 50;
        }

        public ProbeState Initialize()
        {
            // the receiver is configured by platform-specific initializers at the time of protocol execution. prior to this, calls to Initialize won't have access to a Geolocator.
            if (_locator == null)
                return ProbeState.Uninitialized;
            else
            {
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

        public void TakeReading(int timeout, AutoResetEvent readingEvent)
        {
            _locator.GetPositionAsync(timeout: timeout).ContinueWith(t =>
                {
                    Console.Error.WriteLine("Obtained reading from GPS.");
                    _mostRecentReading = t.Result;
                    readingEvent.Set();
                }, TaskScheduler.FromCurrentSynchronizationContext());
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

using Sensus.Exceptions;
using Sensus.UI.Properties;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    public abstract class GpsProbe : ActivePassiveProbe
    {
        private GpsReceiver _gpsReceiver;
        private int _minimumTimeHint;
        private int _minimumDistanceHint;

        protected GpsReceiver GpsReceiver
        {
            get { return _gpsReceiver; }
        }

        [EntryIntegerUiProperty("Passive Res Time (MS):", true)]
        public int MinimumTimeHint
        {
            get { return _minimumTimeHint; }
            set
            {
                if (value != _minimumTimeHint)
                {
                    _minimumTimeHint = value;
                    OnPropertyChanged();

                    // if this is a passive, running probe, restart the listener
                    if (Passive && State == ProbeState.Started)
                    {
                        StopListening();
                        StartListening();
                    }
                }
            }
        }

        [EntryIntegerUiProperty("Passive Res Distance (M):", true)]
        public int MinimumDistanceHint
        {
            get { return _minimumDistanceHint; }
            set
            {
                if (value != _minimumDistanceHint)
                {
                    _minimumDistanceHint = value;
                    OnPropertyChanged();

                    // if this is a passive, running probe, restart the listener
                    if (Passive && State == ProbeState.Started)
                    {
                        StopListening();
                        StartListening();
                    }
                }
            }
        }

        [EntryIntegerUiProperty("Desired Accuracy (M):", true)]
        public int DesiredAccuracyMeters
        {
            get { return _gpsReceiver.DesiredAccuracyMeters; }
            set
            {
                if (value != _gpsReceiver.DesiredAccuracyMeters)
                {
                    _gpsReceiver.DesiredAccuracyMeters = value; // run-time changes are handled in this object -- the value is simply updated on the locator
                    OnPropertyChanged();
                }
            }
        }

        protected GpsProbe()
        {
            _gpsReceiver = new GpsReceiver();

            _gpsReceiver.PositionChanged += (o, e) =>
                {
                    if (Logger.Level >= LoggingLevel.Verbose)
                        Logger.Log("GPS position has changed:  " + e.Position.Latitude + " " + e.Position.Longitude);

                    StoreDatum(ConvertReadingToDatum(e.Position));
                };

            _gpsReceiver.PositionError += (o, e) =>
                {
                    if (Logger.Level >= LoggingLevel.Normal)
                        Logger.Log("GPS receiver position error:  " + e.Error);
                };
        }

        /// <summary>
        /// Should be called by a platform-specific ProbeInitializer to set the Geolocator.
        /// </summary>
        /// <param name="locator">Platform-specific Geolocator.</param>
        public void SetLocator(Geolocator locator)
        {
            _gpsReceiver.Locator = locator;
        }

        public override ProbeState Initialize()
        {
            // base class cannot fully initialize this probe...there is work to be done below.
            if (base.Initialize() != ProbeState.Initializing)
                throw new InvalidProbeStateException(this, ProbeState.Initialized);

            // Initialize can be called from the generic ProbeInitializer, in which case the GPS won't be configured. the GPS is configured by platform-specific initializers like AndroidProbeInitializer.
            if (_gpsReceiver.Initialize() == ProbeState.Initialized)
                ChangeState(ProbeState.Initializing, ProbeState.Initialized);

            return State;
        }

        /// <summary>
        /// Polls for a Datum from this GpsProbe. This is thread-safe, and concurrent calls will block to take new readings.
        /// </summary>
        /// <returns></returns>
        protected override Datum Poll()
        {
            lock (this)
            {
                if (Logger.Level >= LoggingLevel.Verbose)
                    Logger.Log("Polling GPS receiver.");

                return ConvertReadingToDatum(_gpsReceiver.GetReading(10000));
            }
        }

        protected override void StopListening()
        {
            _gpsReceiver.StopListeningForChanges();
        }

        protected abstract Datum ConvertReadingToDatum(Position reading);
    }
}

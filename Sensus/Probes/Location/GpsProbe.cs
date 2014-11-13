using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
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

        [EntryIntegerUiProperty("Min. Time (Milliseconds, Passive Only):", true)]
        public int MinimumTimeHint
        {
            get { return _minimumTimeHint; }
            set
            {
                if (value != _minimumTimeHint)
                {
                    _minimumTimeHint = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Min. Distance (Meters, Passive Only):", true)]
        public int MinimumDistanceHint
        {
            get { return _minimumDistanceHint; }
            set
            {
                if (value != _minimumDistanceHint)
                {
                    _minimumDistanceHint = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Desired Accuracy (Meters):", true)]
        public int DesiredAccuracyMeters
        {
            get { return _gpsReceiver.DesiredAccuracyMeters; }
            set
            {
                if (value != _gpsReceiver.DesiredAccuracyMeters)
                {
                    _gpsReceiver.DesiredAccuracyMeters = value;
                    OnPropertyChanged();
                }
            }
        }

        protected GpsProbe()
        {
            _gpsReceiver = new GpsReceiver();

            _gpsReceiver.PositionChanged += (o, e) =>
                {
                    StoreDatum(ConvertReadingToDatum(e.Position));
                };

            _gpsReceiver.PositionError += (o, e) =>
                {
                    Console.Error.WriteLine("GPS receiver position error:  " + e.Error);
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
            // base class cannot full initialize this probe...there is work to be done below.
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
                return ConvertReadingToDatum(_gpsReceiver.TakeReading(10000));
        }

        protected override void StopListening()
        {
            _gpsReceiver.StopListeningForChanges();
        }

        protected abstract Datum ConvertReadingToDatum(Position reading);
    }
}

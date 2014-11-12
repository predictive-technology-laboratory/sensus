using Sensus.DataStores.Local;
using Sensus.Exceptions;
using Sensus.Probes.Parameters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Probes information from the GPS sensor.
    /// </summary>
    public class GpsLocationPollingProbe : PollingProbe
    {
        private class GpsLocationPollingDatum : Datum
        {
            private double _latitude;
            private double _longitude;

            public GpsLocationPollingDatum(int probeId, DateTimeOffset timestamp, double latitude, double longitude)
                : base(probeId, timestamp)
            {
                _latitude = latitude;
                _longitude = longitude;
            }
        }

        private int _desiredAccuracyMeters;
        private Geolocator _locator;
        private GpsLocationPollingDatum _currentLocation;

        [EntryIntegerProbeParameter("Desired Accuracy (Meters):", true)]
        public int DesiredAccuracyMeters
        {
            get { return _desiredAccuracyMeters; }
            set
            {
                if (value != _desiredAccuracyMeters)
                {
                    _desiredAccuracyMeters = value;
                    OnPropertyChanged();
                }
            }
        }

        public Geolocator Locator
        {
            get { return _locator; }
            set { _locator = value; }
        }

        protected override string DisplayName
        {
            get { return "GPS Location Polling Probe"; }
        }

        public GpsLocationPollingProbe()
        {
            _desiredAccuracyMeters = 10;
            _locator = null;
        }

        public override ProbeState Initialize()
        {
            if (base.Initialize() != ProbeState.Initializing)
                throw new InvalidProbeStateException(this, ProbeState.Initialized);

            if (_locator != null)
                State = ProbeState.Initialized;

            return State;
        }

        protected override Datum Poll()
        {
            GetLocation();
            DataReceivedWaitHandle.WaitOne();
            return _currentLocation;
        }

        private void GetLocation()
        {
            _locator.GetPositionAsync(timeout: 10000).ContinueWith(t =>
                {
                    _currentLocation = new GpsLocationPollingDatum(Id, t.Result.Timestamp, t.Result.Latitude, t.Result.Longitude);
                    DataReceivedWaitHandle.Set();

                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}

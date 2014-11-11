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
    public class GpsLocationProbe : Probe
    {
        private class GpsLocationDatum : Datum
        {
            private double _latitude;
            private double _longitude;

            public GpsLocationDatum(DateTimeOffset timestamp, double latitude, double longitude)
                : base(timestamp)
            {
                _latitude = latitude;
                _longitude = longitude;
            }
        }

        private int _desiredAccuracyMeters;
        private Geolocator _locator;

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

        protected override string FriendlyName
        {
            get { return "GPS Location Probe"; }
        }

        public GpsLocationProbe()
        {
            _desiredAccuracyMeters = 10;
            _locator = null;
        }

        public void Initialize(Geolocator locator)
        {
            if (State != ProbeState.Initializing)
                throw new InvalidProbeStateException(this, ProbeState.Initialized);

            _locator = locator;
            _locator.DesiredAccuracy = _desiredAccuracyMeters;

            State = ProbeState.Initialized;
        }

        public override void Test()
        {
            base.Test();

            Poll();

            DataReceivedWaitHandle.WaitOne();

            if (PolledData.Count != 1)
                throw new ProbeTestException(this, "Failed to get test location");

            PolledData.Clear();
        }

        protected override void Poll()
        {
            _locator.GetPositionAsync(timeout: 10000).ContinueWith(t =>
                {
                    lock (PolledData)
                    {
                        PolledData.Add(new GpsLocationDatum(t.Result.Timestamp, t.Result.Latitude, t.Result.Longitude));
                    }

                    DataReceivedWaitHandle.Set();

                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}

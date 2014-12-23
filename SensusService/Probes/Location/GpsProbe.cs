using System;
using System.Collections.Generic;
using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    public abstract class GpsProbe : ListeningOrPollingProbe
    {
        private EventHandler<PositionEventArgs> _positionChangedHandler;

        protected GpsProbe()
        {
            _positionChangedHandler = (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose);

                    StoreDatum(ConvertReadingToDatum(e.Position));
                };
        }

        /// <summary>
        /// Polls for a Datum from this GpsProbe. This is thread-safe, and concurrent calls will block to take new readings.
        /// </summary>
        /// <returns></returns>
        public sealed override IEnumerable<Datum> Poll()
        {
            lock (this)
            {
                SensusServiceHelper.Get().Logger.Log("Polling GPS receiver.", LoggingLevel.Verbose);

                return new Datum[] { ConvertReadingToDatum(GpsReceiver.Get().GetReading((Controller as PollingProbeController).SleepDurationMS, 10000)) };
            }
        }

        public sealed override void StartListening()
        {
            GpsReceiver.Get().AddListener(_positionChangedHandler);
        }

        public sealed override void StopListening()
        {
            GpsReceiver.Get().RemoveListener(_positionChangedHandler);
        }

        protected abstract Datum ConvertReadingToDatum(Position reading);
    }
}
using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
using Xamarin.Geolocation;

namespace Sensus.Probes.Location
{
    public abstract class GpsProbe : ActivePassiveProbe
    {
        private EventHandler<PositionEventArgs> _positionChangedHandler;

        protected GpsProbe()
        {
            _positionChangedHandler = (o, e) =>
                {
                    if (Passive && State == ProbeState.Started)
                    {
                        if (Logger.Level >= LoggingLevel.Verbose)
                            Logger.Log("Received position change notification.");

                        StoreDatum(ConvertReadingToDatum(e.Position));
                    }
                };
        }

        public override ProbeState Initialize()
        {
            // base class cannot fully initialize this probe...there is work to be done below.
            if (base.Initialize() != ProbeState.Initializing)
                throw new InvalidProbeStateException(this, ProbeState.Initialized);

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

                return ConvertReadingToDatum(GpsReceiver.Get().GetReading(SleepDurationMS, 10000));
            }
        }

        protected override void StartListening()
        {
            GpsReceiver.Get().AddListener(_positionChangedHandler);
        }

        protected override void StopListening()
        {
            GpsReceiver.Get().RemoveListener(_positionChangedHandler);
        }

        protected abstract Datum ConvertReadingToDatum(Position reading);
    }
}

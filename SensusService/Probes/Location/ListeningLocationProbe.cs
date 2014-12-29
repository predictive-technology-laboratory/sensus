using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
        /// <summary>
    /// Probes location information via listening.
    /// </summary>
    public class ListeningLocationProbe : ListeningProbe
    {
        private EventHandler<PositionEventArgs> _positionChangedHandler;

        protected override string DefaultDisplayName
        {
            get { return "Location (Listening)"; }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!GpsReceiver.Get().Locator.IsGeolocationEnabled)
                throw new Exception("Geolocation is not enabled on this device.");
        }

        public ListeningLocationProbe()
        {
            _positionChangedHandler = (o, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose);

                    StoreDatum(new LocationDatum(this, e.Position.Timestamp, e.Position.Accuracy, e.Position.Latitude, e.Position.Longitude));
                };
        }

        protected sealed override void StartListening()
        {
            GpsReceiver.Get().AddListener(_positionChangedHandler);
        }

        protected sealed override void StopListening()
        {
            GpsReceiver.Get().RemoveListener(_positionChangedHandler);
        }
    }
}

// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
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

        public sealed override Type DatumType
        {
            get { return typeof(LocationDatum); }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!GpsReceiver.Get().Locator.IsGeolocationEnabled)
            {
                string error = "Geolocation is not enabled on this device. Cannot start location probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
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

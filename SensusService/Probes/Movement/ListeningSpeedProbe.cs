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
using SensusService.Probes.Location;

namespace SensusService.Probes.Movement
{
    public class ListeningSpeedProbe : ListeningProbe
    {        
        private EventHandler<PositionEventArgs> _positionChangedHandler;
        private Position _previousPosition;

        private readonly object _locker = new object();

        protected sealed override string DefaultDisplayName
        {
            get
            {
                return "Speed";
            }
        }

        public sealed override Type DatumType
        {
            get
            {
                return typeof(SpeedDatum);
            }
        }

        public ListeningSpeedProbe()
        {
            _positionChangedHandler = (o, e) =>
            {
                lock (_locker)
                {
                    SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

                    if (_previousPosition != null && e.Position != null && e.Position.Timestamp != _previousPosition.Timestamp)
                        StoreDatum(new SpeedDatum(e.Position.Timestamp, _previousPosition, e.Position));

                    if (e.Position != null)
                        _previousPosition = e.Position;
                }
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!GpsReceiver.Get().Locator.IsGeolocationEnabled)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not enabled on this device. Cannot start speed probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override void StartListening()
        {
            _previousPosition = null;
            GpsReceiver.Get().AddListener(_positionChangedHandler);
        }

        protected sealed override void StopListening()
        {
            GpsReceiver.Get().RemoveListener(_positionChangedHandler);
            _previousPosition = null;
        }
    }
}
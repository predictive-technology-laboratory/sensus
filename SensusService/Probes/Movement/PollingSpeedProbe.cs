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

using SensusService.Probes.Location;
using System;
using System.Collections.Generic;
using Xamarin.Geolocation;
using System.Threading;

namespace SensusService.Probes.Movement
{
    public class PollingSpeedProbe : PollingProbe
    {
        private Position _previousPosition;

        private readonly object _locker = new object();

        protected sealed override string DefaultDisplayName
        {
            get { return "Speed"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(SpeedDatum); }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 15000; // every 15 seconds
            }
        }

        public PollingSpeedProbe()
        {
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

        public override void Start()
        {
            lock (_locker)
            {
                _previousPosition = null;  // do this before starting the base-class poller so it doesn't race to grab a stale previous location.
                base.Start();               
            }
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                Position currentPosition = null;

                try
                {
                    currentPosition = GpsReceiver.Get().GetReading(cancellationToken);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to get GPS reading:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                SpeedDatum datum = null;

                if (currentPosition != null)
                {
                    if (_previousPosition == null)
                        _previousPosition = currentPosition;
                    else if (currentPosition.Timestamp > _previousPosition.Timestamp)  // it has happened (rarely) that positions come in out of order...drop any such positions.
                    {
                        datum = new SpeedDatum(currentPosition.Timestamp, _previousPosition, currentPosition);
                        _previousPosition = currentPosition;
                    }
                }

                if (datum == null)
                    return new Datum[] { };
                else
                    return new Datum[] { datum };
            }
        }

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();
                _previousPosition = null;  // reset previous location so it doesn't get used when this probe is restarted.
            }
        }
    }
}
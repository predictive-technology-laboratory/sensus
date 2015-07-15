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
using System.Collections.Generic;
using Xamarin.Geolocation;
using System.Threading;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// Probes location information.
    /// </summary>
    public class PollingLocationProbe : PollingProbe
    {
        protected sealed override string DefaultDisplayName
        {
            get { return "Location"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 5000; }
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

        protected sealed override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            Position reading = GpsReceiver.Get().GetReading(cancellationToken);

            if (reading == null)
                return new Datum[] { };
            else
                return new Datum[] { new LocationDatum(reading.Timestamp, reading.Accuracy, reading.Latitude, reading.Longitude) };
        }
    }
}
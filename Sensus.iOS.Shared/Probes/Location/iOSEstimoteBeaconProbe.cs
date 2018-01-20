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
using Sensus.Probes.Location;
using Estimote.iOS.Proximity;
using System.Collections.Generic;
using Sensus.Context;

namespace Sensus.iOS.Probes.Location
{
    public class iOSEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private EPXProximityObserver _observer;

        public iOSEstimoteBeaconProbe()
        {
        }

        protected override void StartListening()
        {
            _observer = new EPXProximityObserver(new EPXCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken), error => 
            {                
                SensusServiceHelper.Get().Logger.Log("Error while initializing proximiy observer:  " + error, LoggingLevel.Normal, GetType());
            });

            List<EPXProximityZone> zones = new List<EPXProximityZone>();

            foreach (EstimoteBeacon beacon in Beacons)
            {
                EPXProximityZone zone = new EPXProximityZone(new EPXProximityRange(beacon.ProximityMeters), "sensus", beacon.Name);

                zone.OnEnterAction = async (triggeringDeviceAttachment) =>
                {
                    await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Entered));
                };

                zone.OnExitAction = async (triggeringDeviceAttachment) =>
                {
                    await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Exited));
                };

                zones.Add(zone);
            }

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _observer.StartObservingZones(zones.ToArray());
            });
        }

        protected override void StopListening()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _observer.StopObservingZones();
            });
        }
    }
}

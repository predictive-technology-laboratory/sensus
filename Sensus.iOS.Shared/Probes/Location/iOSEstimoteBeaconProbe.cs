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
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Location
{
    public class iOSEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private ProximityObserver _observer;

        protected override Task StartListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _observer = new ProximityObserver(new CloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken), error =>
                {
                    SensusServiceHelper.Get().Logger.Log("Error while initializing proximity observer:  " + error, LoggingLevel.Normal, GetType());
                });

                List<ProximityZone> zones = new List<ProximityZone>();

                foreach (EstimoteBeacon beacon in Beacons)
                {
                    ProximityZone zone = new ProximityZone(beacon.Tag, new ProximityRange(beacon.ProximityMeters))
                    {
                        OnEnter = async (triggeringDeviceAttachment) =>
                        {
                            await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Entered));
                        },

                        OnExit = async (triggeringDeviceAttachment) =>
                        {
                            await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Exited));
                        }
                    };

                    zones.Add(zone);
                }

                _observer.StartObservingZones(zones.ToArray());
            });

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(_observer.StopObservingZones);

            return Task.CompletedTask;
        }
    }
}
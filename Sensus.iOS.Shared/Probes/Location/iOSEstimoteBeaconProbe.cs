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

namespace Sensus.iOS.Probes.Location
{
    /*public class iOSEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private EPXProximityObserver _proximityObserver;

        public iOSEstimoteBeaconProbe()
        {
        }

        protected override void StartListening()
        {

            _proximityObserver = new ProximityObserverBuilder(Application.Context, new EPXCloudCredentials())
                .WithBalancedPowerMode()
                .WithTelemetryReporting()
                .WithScannerInForegroundService(notification)
                .WithOnErrorAction(new ErrorHandler())
                .Build();

            List<IProximityZone> zones = new List<IProximityZone>();

            foreach (EstimoteBeacon beacon in Beacons)
            {
                IProximityZone zone = _proximityObserver.ZoneBuilder()
                                                        .ForAttachmentKeyAndValue("sensus", beacon.Name)
                                                        .InCustomRange(beacon.ProximityMeters)
                                                        .WithOnEnterAction(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Entered))
                                                        .WithOnExitAction(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Exited))
                                                        .Create();

                zones.Add(zone);
            }

            _proximityObservationHandler = _proximityObserver.AddProximityZones(zones.ToArray())
                                                             .Start();
        }

        protected override void StopListening()
        {
            _proximityObservationHandler.Stop();
        }
    }*/
}

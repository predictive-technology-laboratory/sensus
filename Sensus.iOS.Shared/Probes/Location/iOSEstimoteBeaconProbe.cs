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
using Estimote.iOS.Indoor;

namespace Sensus.iOS.Probes.Location
{
    public class iOSEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private ProximityObserver _observer;
        private EILBackgroundIndoorLocationManager _backgroundIndoorLocationManager;
        private List<EILLocation> _indoorLocationsBeingMonitored;

        private iOSEstimoteBeaconProbe()
        {
            _indoorLocationsBeingMonitored = new List<EILLocation>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (Locations.Count > 0)
            {
                _backgroundIndoorLocationManager = new EILBackgroundIndoorLocationManager();

                EstimoteBackgroundIndoorLocationManagerDelegate locationManagerDelegate = new EstimoteBackgroundIndoorLocationManagerDelegate();

                locationManagerDelegate.UpdatedPositionAsync += async (position, accuracy, location) =>
                {
                    await StoreDatumAsync(new EstimoteIndoorLocationDatum(DateTimeOffset.UtcNow, position.X, position.Y, accuracy.ToString(), location.Name, location.Identifier));
                };

                _backgroundIndoorLocationManager.Delegate = locationManagerDelegate;
            }
        }

        protected override Task StartListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                if (Beacons.Count > 0)
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
                }

                if (Locations.Count > 0)
                {
                    foreach (EstimoteLocation location in Locations)
                    {
                        EILRequestFetchLocation locationFetchRequest = new EILRequestFetchLocation(location.Identifier);

                        locationFetchRequest.SendRequest((fetchedLocation, error) =>
                        {
                            if (error == null)
                            {
                                _backgroundIndoorLocationManager.StartPositionUpdatesForLocation(fetchedLocation);

                                lock (_indoorLocationsBeingMonitored)
                                {
                                    _indoorLocationsBeingMonitored.Add(fetchedLocation);
                                }
                            }
                            else
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to fetch Estimote location:  " + error, LoggingLevel.Normal, GetType());
                            }
                        });
                    }
                }
            });

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            if (Beacons.Count > 0)
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(_observer.StopObservingZones);
            }

            lock (_indoorLocationsBeingMonitored)
            {
                if (_indoorLocationsBeingMonitored.Count > 0)
                {
                    foreach (EILLocation location in _indoorLocationsBeingMonitored)
                    {
                        _backgroundIndoorLocationManager.StopPositionUpdatesForLocation(location);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
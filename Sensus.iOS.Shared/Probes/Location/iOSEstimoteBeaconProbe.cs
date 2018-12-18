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
        private ProximityObserver _proximityObserver;
        private EILIndoorLocationManager _foregroundIndoorLocationManager;
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
                // foreground monitoring
                _foregroundIndoorLocationManager = new EILIndoorLocationManager();

                EstimoteForegroundIndoorLocationManagerDelegate foregroundLocationManagerDelegate = new EstimoteForegroundIndoorLocationManagerDelegate();

                foregroundLocationManagerDelegate.UpdatedPositionAsync += async (position, accuracy, location) =>
                {
                    EstimoteIndoorLocationAccuracy accuracyEnum = (EstimoteIndoorLocationAccuracy)Enum.Parse(typeof(EstimoteIndoorLocationAccuracy), accuracy.ToString(), true);
                    await StoreDatumAsync(new EstimoteIndoorLocationDatum(DateTimeOffset.UtcNow, position.X, position.Y, accuracyEnum, location.Name, location.Identifier));
                };

                _foregroundIndoorLocationManager.Delegate = foregroundLocationManagerDelegate;

                // background monitoring
                _backgroundIndoorLocationManager = new EILBackgroundIndoorLocationManager();

                EstimoteBackgroundIndoorLocationManagerDelegate backgroundLocationManagerDelegate = new EstimoteBackgroundIndoorLocationManagerDelegate();

                backgroundLocationManagerDelegate.UpdatedPositionAsync += async (position, accuracy, location) =>
                {
                    EstimoteIndoorLocationAccuracy accuracyEnum = (EstimoteIndoorLocationAccuracy)Enum.Parse(typeof(EstimoteIndoorLocationAccuracy), accuracy.ToString(), true);
                    await StoreDatumAsync(new EstimoteIndoorLocationDatum(DateTimeOffset.UtcNow, position.X, position.Y, accuracyEnum, location.Name, location.Identifier));
                };

                _backgroundIndoorLocationManager.Delegate = backgroundLocationManagerDelegate;
            }
        }

        protected override Task StartListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                if (Beacons.Count > 0)
                {
                    _proximityObserver = new ProximityObserver(new CloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken), error =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Error while initializing proximity observer:  " + error, LoggingLevel.Normal, GetType());
                    });

                    List<ProximityZone> beaconZones = new List<ProximityZone>();

                    foreach (EstimoteBeacon beacon in Beacons)
                    {
                        ProximityZone beaconZone = new ProximityZone(beacon.Tag, new ProximityRange(beacon.ProximityMeters))
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

                        beaconZones.Add(beaconZone);
                    }

                    _proximityObserver.StartObservingZones(beaconZones.ToArray());
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
                                _foregroundIndoorLocationManager.StartPositionUpdatesForLocation(fetchedLocation);
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
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(_proximityObserver.StopObservingZones);
            }

            lock (_indoorLocationsBeingMonitored)
            {
                if (_indoorLocationsBeingMonitored.Count > 0)
                {
                    foreach (EILLocation location in _indoorLocationsBeingMonitored)
                    {
                        _foregroundIndoorLocationManager.StopPositionUpdates();
                        _backgroundIndoorLocationManager.StopPositionUpdatesForLocation(location);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
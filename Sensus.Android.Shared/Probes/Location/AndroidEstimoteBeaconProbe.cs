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
using Estimote.Android.Proximity;
using Android.App;
using Sensus.Context;
using System.Collections.Generic;
using Sensus.Probes.Location;
using Sensus.Android.Notifications;
using System.Threading.Tasks;
using Estimote.Android.Indoor;

// the unit test project contains the Resource class in its namespace rather than the Sensus.Android
// namespace. include that namespace below.
#if UNIT_TEST
using Sensus.Android.Tests;
#endif

namespace Sensus.Android.Probes.Location
{
    public class AndroidEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private class ProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public AndroidEstimoteBeaconProbe _probe;
            public EstimoteBeacon _beacon;
            public EstimoteBeaconProximityEvent _proximityEvent;

            public ProximityHandler(AndroidEstimoteBeaconProbe probe, EstimoteBeacon beacon, EstimoteBeaconProximityEvent proximityEvent)
            {
                _probe = probe;
                _beacon = beacon;
                _proximityEvent = proximityEvent;
            }

            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                InvokeAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, _beacon, _proximityEvent));
                return null;
            }

            /// <summary>
            /// Re-declared <see cref="Invoke(Java.Lang.Object)"/> to properly await result.
            /// </summary>
            /// <param name="datum">Datum.</param>
            private async void InvokeAsync(EstimoteBeaconDatum datum)
            {
                await _probe.StoreDatumAsync(datum);
            }
        }

        private class ErrorHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                SensusServiceHelper.Get().Logger.Log("Error while observing Estimote beacons:  " + p0, LoggingLevel.Normal, GetType());
                return null;
            }
        }

        IProximityObserver _proximityObserver;
        IProximityObserverHandler _proximityObservationHandler;
        List<IScanningIndoorLocationManager> _indoorLocationManagers;

        protected override Task InitializeAsync()
        {
            await base.InitializeAsync();

            _indoorLocationManagers = new List<IScanningIndoorLocationManager>();
        }

        protected override async Task StartListeningAsync()
        {
            if (Beacons.Count > 0)
            {
                Notification notification = (SensusContext.Current.Notifier as AndroidNotifier).CreateNotificationBuilder(Application.Context, AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                                                               .SetSmallIcon(Resource.Drawable.notification_icon_background)
                                                                                               .SetContentTitle("Beacon Scan")
                                                                                               .SetContentText("Scanning...")
                                                                                               .SetOngoing(true)
                                                                                               .Build();

                _proximityObserver = new ProximityObserverBuilder(Application.Context, new EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken))
                    .WithBalancedPowerMode()
                    .WithScannerInForegroundService(notification)
                    .OnError(new ErrorHandler())
                    .Build();

                List<IProximityZone> zones = new List<IProximityZone>();

                foreach (EstimoteBeacon beacon in Beacons)
                {
                    IProximityZone zone = new ProximityZoneBuilder()
                                                  .ForTag(beacon.Tag)
                                                  .InCustomRange(beacon.ProximityMeters)
                                                  .OnEnter(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Entered))
                                                  .OnExit(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Exited))
                                                  .Build();

                    zones.Add(zone);
                }

                _proximityObservationHandler = _proximityObserver.StartObserving(zones);
            }

            if (Locations.Count > 0)
            {
                EstimoteCloudCredentials credentials = new Estimote.Android.Indoor.EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken);

                foreach (EstimoteLocation location in Locations)
                {
                    IIndoorCloudManager indoorCloudManager = new IndoorCloudManagerFactory().Create(global::Android.App.Application.Context, credentials);
                    AndroidEstimoteIndoorCloudCallback cloudCallback = new AndroidEstimoteIndoorCloudCallback();
                    indoorCloudManager.GetLocation(location.Identifier, cloudCallback);
                    Estimote.Android.Indoor.Location cloudLocation = await cloudCallback.GetValueAsync();

                    IScanningIndoorLocationManager indoorLocationManager = new IndoorLocationManagerBuilder(global::Android.App.Application.Context, cloudLocation, credentials)
                        .WithPositionUpdateInterval(IndoorLocationUpdateInterval.TotalMilliseconds)
                        .WithOnErrorAction(new ErrorHandler())
                        .WithScannerInForegroundService(notification)
                        .Build();

                    AndroidEstimoteIndoorPositionUpdateListener indoorPositionUpdateListener = new AndroidEstimoteIndoorPositionUpdateListener();
                    indoorPositionUpdateListener.UpdatedPosition += async (sender, estimoteLocation) =>
                    {
                        EstimoteIndoorLocationDatum datum = null;

                        if (estimoteLocation != null)
                        {
                            datum = new EstimoteIndoorLocationDatum(DateTimeOffset.UtcNow, estimoteLocation.GetX(), estimoteLocation.GetY(), EstimoteIndoorLocationAccuracy.Unknown, location.Name, location.Identifier)
                        }

                        await StoreDatumAsync(datum);
                    };

                    indoorLocationManager.SetOnPositionUpdateListener(indoorPositionUpdateListener);
                    indoorLocationManager.StartPositioning();

                    lock (_indoorLocationManagers)
                    {
                        _indoorLocationManagers.Add(indoorLocationManager);
                    }
                }
            }
        }

        protected override Task StopListeningAsync()
        {
            if (Beacons.Count > 0)
            {
                _proximityObservationHandler.Stop();
            }

            lock (_indoorLocationManagers)
            {
                if (_indoorLocationManagers.Count > 0)
                {
                    foreach (IScanningIndoorLocationManager indoorLocationManager in _indoorLocationManagers)
                    {
                        indoorLocationManager.StopPositioning();
                    }

                    _indoorLocationManagers.Clear();
                }
            }

            return Task.CompletedTask;
        }
    }
}
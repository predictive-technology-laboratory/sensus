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

extern alias indoor;
extern alias proximity;

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
        private class ProximityHandler : Java.Lang.Object, proximity::Kotlin.Jvm.Functions.IFunction1
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

        private class ProximityErrorHandler : Java.Lang.Object, proximity::Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                SensusServiceHelper.Get().Logger.Log("Error while observing Estimote beacons:  " + p0, LoggingLevel.Normal, GetType());
                return null;
            }
        }

        private class IndoorErrorHandler : Java.Lang.Object, indoor::Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                SensusServiceHelper.Get().Logger.Log("Error during indoor positioning:  " + p0, LoggingLevel.Normal, GetType());
                return null;
            }
        }

        IProximityObserver _proximityObserver;
        IProximityObserverHandler _proximityObservationHandler;
        IScanningIndoorLocationManager _indoorLocationManager;

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            Notification notification = (SensusContext.Current.Notifier as AndroidNotifier).CreateNotificationBuilder(AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                                                               .SetSmallIcon(Resource.Drawable.notification_icon_background)
                                                                                               .SetContentTitle("Beacon Scan")
                                                                                               .SetContentText("Scanning...")
                                                                                               .SetOngoing(true)
                                                                                               .Build();
            if (Beacons.Count > 0)
            {
                _proximityObserver = new ProximityObserverBuilder(Application.Context, new Estimote.Android.Proximity.EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken))
                                             .WithBalancedPowerMode()
                                             .WithScannerInForegroundService(notification)
                                             .OnError(new ProximityErrorHandler())
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

            if (Location != null)
            {
                Estimote.Android.Indoor.EstimoteCloudCredentials credentials = new Estimote.Android.Indoor.EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken);

                IIndoorCloudManager indoorCloudManager = new IndoorCloudManagerFactory().Create(Application.Context, credentials);
                AndroidEstimoteIndoorCloudCallback cloudCallback = new AndroidEstimoteIndoorCloudCallback();
                indoorCloudManager.GetLocation(Location.Identifier, cloudCallback);
                Estimote.Android.Indoor.Location cloudLocation = await cloudCallback.GetValueAsync();

                _indoorLocationManager = new IndoorLocationManagerBuilder(Application.Context, cloudLocation, credentials)
                                                 .WithPositionUpdateInterval(IndoorLocationUpdateIntervalMS)
                                                 .WithOnErrorAction(new IndoorErrorHandler())
                                                 .WithScannerInForegroundService(notification)
                                                 .Build();

                AndroidEstimoteIndoorPositionUpdateListener indoorPositionUpdateListener = new AndroidEstimoteIndoorPositionUpdateListener();
                indoorPositionUpdateListener.UpdatedPositionAsync += async (estimoteLocation) =>
                {
                    EstimoteIndoorLocationDatum datum = null;

                    if (estimoteLocation != null)
                    {
                        datum = new EstimoteIndoorLocationDatum(DateTimeOffset.UtcNow, estimoteLocation.GetX(), estimoteLocation.GetY(), estimoteLocation.Orientation, EstimoteIndoorLocationAccuracy.Unknown, Location.Name, Location.Identifier, cloudLocation, estimoteLocation);
                    }

                    await StoreDatumAsync(datum);
                };

                _indoorLocationManager.SetOnPositionUpdateListener(indoorPositionUpdateListener);
                _indoorLocationManager.StartPositioning();
            }
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

            if (Beacons.Count > 0)
            {
                _proximityObservationHandler.Stop();
            }

            if (_indoorLocationManager != null)
            {
                _indoorLocationManager.StopPositioning();
            }
        }
    }
}
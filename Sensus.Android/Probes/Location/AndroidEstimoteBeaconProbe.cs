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
using Estimote.Android.Cloud;
using Android.App;
using Sensus.Android;
using Sensus.Context;
using System.Collections.Generic;
using Sensus.Probes.Location;
using Sensus.Android.Callbacks;

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
                _probe.StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, _beacon.Name, _beacon.ProximityMeters, _proximityEvent));
                return null;
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

        public AndroidEstimoteBeaconProbe()
        {
        }

        protected override void StartListening()
        {
            Notification notification = (SensusContext.Current.Notifier as AndroidNotifier).CreateNotificationBuilder(Application.Context, AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                                                           .SetSmallIcon(Resource.Drawable.notification_icon_background)
                                                                                           .SetContentTitle("Beacon Scan")
                                                                                           .SetContentText("Scanning...")
                                                                                           .SetOngoing(true)
                                                                                           .Build();

            _proximityObserver = new ProximityObserverBuilder(Application.Context, new EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken))
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
    }
}
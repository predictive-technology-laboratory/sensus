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

namespace Sensus.Android.Probes.Location
{
    public class AndroidEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private class EnterProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                return null;
            }
        }

        private class ExitProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                return null;
            }
        }

        private class ErrorHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
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
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                Notification notification = new Notification.Builder(Application.Context)
                                                            .SetSmallIcon(Resource.Drawable.ic_launcher)
                                                            .SetContentTitle("Beacon scan")
                                                            .SetContentText("Scan is running...")
                                                            .Build();

                _proximityObserver = new ProximityObserverBuilder(Application.Context, new EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken))
                    .WithBalancedPowerMode()
                    .WithTelemetryReporting()
                    //.WithScannerInForegroundService(notification)
                    .WithOnErrorAction(new ErrorHandler())
                    .Build();

                List<IProximityZone> zones = new List<IProximityZone>();

                foreach (EstimoteBeacon beacon in Beacons)
                {
                    IProximityZone zone = _proximityObserver
                        .ZoneBuilder()
                        .ForAttachmentKeyAndValue("sensus", beacon.Identifier)
                        .InCustomRange(beacon.ProximityMeters)
                        .WithOnEnterAction(new EnterProximityHandler())
                        .WithOnExitAction(new ExitProximityHandler())
                        .Create();

                    zones.Add(zone);
                }

                _proximityObservationHandler = _proximityObserver
                    .AddProximityZones(zones.ToArray())
                    .Start();
            });
        }

        protected override void StopListening()
        {
            _proximityObservationHandler.Stop();
        }
    }
}
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
using SensusService.Probes.Movement;
using CoreMotion;
using Foundation;
using System.Threading;
using SensusService;

namespace Sensus.iOS.Probes.Movement
{
    public class iOSAccelerometerProbe : AccelerometerProbe
    {
        private CMMotionManager _motionManager;

        public iOSAccelerometerProbe()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Sensors) == PermissionStatus.Granted)
                _motionManager = new CMMotionManager();
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Motion sensors are not permitted on this device. Cannot start accelerometer probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void StartListening()
        {
            base.StartListening();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    _motionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
                        {
                            if (!Stabilizing)
                                StoreDatum(new AccelerometerDatum(DateTimeOffset.UtcNow, data.Acceleration.X, data.Acceleration.Y, data.Acceleration.Z));
                        });
                });
        }

        protected override void StopListening()
        {
            _motionManager.StopAccelerometerUpdates();
        }
    }
}
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
using Sensus.Probes.Movement;
using CoreMotion;
using Foundation;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Movement
{
    public class iOSGyroScoperProbe : GyroscopeProbe
    {
        private CMMotionManager _motionManager;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Sensors) == PermissionStatus.Granted)
            {
                _motionManager = new CMMotionManager();
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "This device does not contain an accelerometer, or the user has denied access to it. Cannot start accelerometer probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _motionManager?.StartGyroUpdates(new NSOperationQueue(), async (data, error) =>
            {
                if (!Stabilizing && data != null && error == null)
                {
                    await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, data.RotationRate.x, data.RotationRate.y, data.RotationRate.z));
                }
            });
        }

        protected override Task StopListeningAsync()
        {
            _motionManager?.StopGyroUpdates();

            return Task.CompletedTask;
        }
    }
}
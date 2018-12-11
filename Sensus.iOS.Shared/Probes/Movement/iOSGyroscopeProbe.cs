//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Sensus.Probes.Movement;
using CoreMotion;
using Foundation;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Movement
{
    public class iOSGyroscopeProbe : GyroscopeProbe
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
                string error = "This device does not contain an gyroscope, or the user has denied access to it. Cannot start gyroscope probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override Task StartListeningAsync()
        {          
            _motionManager?.StartGyroUpdates(new NSOperationQueue(), async (data, error) =>
            {
                if (data != null && error == null)
                {
                    await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, data.RotationRate.x, data.RotationRate.y, data.RotationRate.z));
                }
            });

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _motionManager?.StopGyroUpdates();

            return Task.CompletedTask;
        }
    }
}

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

namespace Sensus.iOS.Probes.Movement
{
    public class iOSAccelerometerProbe : AccelerometerProbe
    {
        private CMMotionManager _motionManager;

        public iOSAccelerometerProbe()
        {
            _motionManager = new CMMotionManager();
        }

        protected override void StartListening()
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    _motionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
                        {
                            // TODO:  Doesn't fire in simulator.
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
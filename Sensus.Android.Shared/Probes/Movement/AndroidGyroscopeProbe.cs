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

using Android.Hardware;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidGyroscopeProbe : GyroscopeProbe
    {
        private AndroidSensorListener _gyroscopeListener;

        public AndroidGyroscopeProbe()
        {
            _gyroscopeListener = new AndroidSensorListener(SensorType.Gyroscope, async e =>
            {
                if (e.Values.Count >= 3)
                {
                    await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, e.Values[0], e.Values[1], e.Values[2]));
                }
            });
        }

        protected override async Task ProtectedInitializeAsync()
        {
            await base.ProtectedInitializeAsync();

            _gyroscopeListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _gyroscopeListener.Start();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

            _gyroscopeListener.Stop();
        }
    }
}
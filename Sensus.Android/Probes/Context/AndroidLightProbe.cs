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
using Sensus.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    public class AndroidLightProbe : LightProbe
    {
        private AndroidSensorListener _lightListener;

        public AndroidLightProbe()
        {
            _lightListener = new AndroidSensorListener(SensorType.Light, SensorDelay.Normal, null, async e =>
            {
                await StoreDatumAsync(new LightDatum(DateTimeOffset.UtcNow, e.Values[0]));
            });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _lightListener.Initialize();
        }

        protected override void StartListening()
        {
            _lightListener.Start();
        }

        protected override void StopListening()
        {
            _lightListener.Stop();
        }
    }
}

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
using Empatica.iOS;

namespace Sensus.iOS.Probes.User
{
    public class iOSEmpaticaDeviceListener : EmpaticaDeviceDelegate
    {
        public event EventHandler<Tuple<sbyte, sbyte, sbyte, DateTimeOffset>> Acceleration;

        public iOSEmpaticaDeviceListener()
        {
        }

        public override void DidReceiveAccelerationX(sbyte x, sbyte y, sbyte z, double timestamp, EmpaticaDeviceManager device)
        {
            Acceleration(this, new Tuple<sbyte, sbyte, sbyte, DateTimeOffset>());
        }
    }
}


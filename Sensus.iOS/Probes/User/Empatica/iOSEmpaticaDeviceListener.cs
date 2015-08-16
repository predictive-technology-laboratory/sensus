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
using SensusService.Probes.User.Empatica;

namespace Sensus.iOS.Probes.User.Empatica
{
    public class iOSEmpaticaDeviceListener : EmpaticaDeviceListener
    {
        private EmpaticaWristbandProbe _probe;
        private event EventHandler<DeviceStatus> StatusReceived;

        public iOSEmpaticaDeviceListener(EmpaticaWristbandProbe probe)
        {
            _probe = probe;
        }

        public override void DidReceiveAcceleration(sbyte x, sbyte y, sbyte z, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveBatteryLevel(float level, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveBloodVolumePulse(float bloodVolumePulse, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveGalvanicSkinResponse(float galvanicSkinResponse, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveInterBeatInterval(float interBeatInterval, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveTagAtTimestamp(double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidReceiveTemperature(float temperature, double timestamp, EmpaticaDevice device)
        {
        }

        public override void DidUpdateDeviceStatus(DeviceStatus status, EmpaticaDevice device)
        {
            if (StatusReceived != null)
                StatusReceived(this, status);
        }

        private DateTimeOffset ToDateTimeOffset(double timestamp)
        {
            return new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddSeconds(timestamp);
        }
    }
}
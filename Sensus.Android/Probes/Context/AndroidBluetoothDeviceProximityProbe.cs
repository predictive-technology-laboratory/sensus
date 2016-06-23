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

using SensusService.Probes.Context;
using System;
using SensusService;
using Plugin.Permissions.Abstractions;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private EventHandler<BluetoothDeviceProximityDatum> _deviceFoundCallback;

        public AndroidBluetoothDeviceProximityProbe()
        {
            _deviceFoundCallback = (sender, bluetoothDeviceProximityDatum) =>
                {
                    StoreDatum(bluetoothDeviceProximityDatum);
                };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void StartListening()
        {
            AndroidBluetoothBroadcastReceiver.DEVICE_FOUND += _deviceFoundCallback;
        }

        protected override void StopListening()
        {
            AndroidBluetoothBroadcastReceiver.DEVICE_FOUND -= _deviceFoundCallback;
        }
    }
}

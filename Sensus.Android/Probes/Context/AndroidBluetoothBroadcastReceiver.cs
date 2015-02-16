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

using Android.App;
using Android.Bluetooth;
using Android.Content;
using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { BluetoothDevice.ActionFound }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidBluetoothBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<BluetoothDeviceProximityDatum> DeviceFound;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (DeviceFound != null && intent != null && intent.Action == BluetoothDevice.ActionFound)
            {
                BluetoothDevice device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                DeviceFound(this, new BluetoothDeviceProximityDatum(null, DateTimeOffset.UtcNow, device.Name, device.Address));
            }
        }
    }
}

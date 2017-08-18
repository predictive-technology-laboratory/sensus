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
using Android.Bluetooth;
using Java.Util;
using Sensus.Probes.Context;
using System.Text;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothGattClientCallback : BluetoothGattCallback
    {
        public event EventHandler<string> DeviceIdEncountered;

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                gatt.DiscoverServices();
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            UUID serviceUUID = UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_SERVICE_UUID);
            BluetoothGattService service = gatt.GetService(serviceUUID);
            UUID deviceIdCharacteristicUUID = UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_CHARACTERISTIC_UUID);
            BluetoothGattCharacteristic deviceIdCharacteristic = service.GetCharacteristic(deviceIdCharacteristicUUID);
            gatt.ReadCharacteristic(deviceIdCharacteristic);
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);

            byte[] deviceIdBytes = characteristic.GetValue();
            string deviceIdEncountered = Encoding.UTF8.GetString(deviceIdBytes);
            DeviceIdEncountered?.Invoke(this, deviceIdEncountered);

            gatt.Disconnect();
        }
    }
}

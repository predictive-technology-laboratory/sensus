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
        public event EventHandler<BluetoothDeviceProximityDatum> DeviceIdEncountered;

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                try
                {
                    gatt.DiscoverServices();
                }
                catch(Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while discovering services:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            BluetoothGattService deviceIdService;
            try
            {
                UUID deviceIdServiceUUID = UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_SERVICE_UUID);
                deviceIdService = gatt.GetService(deviceIdServiceUUID);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID service:  " + ex, LoggingLevel.Normal, GetType());
                return;
            }

            BluetoothGattCharacteristic deviceIdCharacteristic;
            try
            {
                UUID deviceIdCharacteristicUUID = UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_CHARACTERISTIC_UUID);
                deviceIdCharacteristic = deviceIdService.GetCharacteristic(deviceIdCharacteristicUUID);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID characteristic:  " + ex, LoggingLevel.Normal, GetType());
                return;
            }

            try
            {
                gatt.ReadCharacteristic(deviceIdCharacteristic);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while reading device ID characteristic:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);

            try
            {
                byte[] deviceIdBytes = characteristic.GetValue();
                string deviceIdEncountered = Encoding.UTF8.GetString(deviceIdBytes);
                DeviceIdEncountered?.Invoke(this, new BluetoothDeviceProximityDatum(DateTimeOffset.UtcNow, deviceIdEncountered));
                gatt.Disconnect();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID characteristic value after reading it:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}

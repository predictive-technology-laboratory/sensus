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
using System.Text;
using Android.Bluetooth;
using Java.Util;
using Sensus.Probes.Context;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothGattServerCallback : BluetoothGattServerCallback
    {
        private byte[] _deviceIdValue;

        public BluetoothGattServer Server { get; set; }

        public AndroidBluetoothGattServerCallback()
        {
            _deviceIdValue = Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId);
        }

        public override void OnServiceAdded(ProfileState status, BluetoothGattService service)
        {
            base.OnServiceAdded(status, service);

            SensusServiceHelper.Get().Logger.Log("Service added status:  " + status, LoggingLevel.Normal, GetType());
        }

        public override void OnCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic)
        {
            base.OnCharacteristicReadRequest(device, requestId, offset, characteristic);

            try
            {
                if (characteristic.Service.Uuid == UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_SERVICE_UUID) &&
                    characteristic.Uuid == UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_CHARACTERISTIC_UUID))
                {

                    Server?.SendResponse(device, requestId, GattStatus.Success, offset, _deviceIdValue);
                }
                else
                {
                    Server?.SendResponse(device, requestId, GattStatus.RequestNotSupported, offset, null);
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while sending response:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }
    }
}

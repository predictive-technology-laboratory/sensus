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
using Sensus.Context;
using Sensus.Exceptions;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Android BLE GATT server callback. Serves requests from clients who wish to read the device ID characteristic.
    /// </summary>
    public class AndroidBluetoothServerGattCallback : BluetoothGattServerCallback
    {
        public BluetoothGattServer Server { get; set; }

        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;

        public AndroidBluetoothServerGattCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
        }

        public override void OnServiceAdded(GattStatus status, BluetoothGattService service)
        {
            SensusServiceHelper.Get().Logger.Log("Service added status:  " + status, LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Called when a client wishes to read a characteristic from the service.
        /// </summary>
        /// <param name="device">Client device making the read request.</param>
        /// <param name="requestId">Request identifier.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="characteristic">Characteristic to read.</param>
        public override void OnCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic)
        {
            try
            {
                if (Server == null)
                {
                    SensusException.Report("Null server when responding to BLE characteristic read request.");
                }

                // only respond to client requests for the service and characteristic we are expecting
                if (characteristic.Service.Uuid == _service.Uuid && characteristic.Uuid == _characteristic.Uuid)
                {
                    Server?.SendResponse(device, requestId, GattStatus.Success, offset, _characteristic.GetValue());
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

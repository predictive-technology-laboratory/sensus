//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Android.Bluetooth;
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
            base.OnServiceAdded(status, service);

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
            base.OnCharacteristicReadRequest(device, requestId, offset, characteristic);

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

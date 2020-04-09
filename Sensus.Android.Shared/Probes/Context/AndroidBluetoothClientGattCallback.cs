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
using System.Text;
using Sensus.Probes.Context;
using System.Threading.Tasks;
using System.Threading;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Android BLE GATT client callback. Makes requests to servers to read a characteristic upon encountering a service.
    /// </summary>
    public class AndroidBluetoothClientGattCallback : BluetoothGattCallback
    {
        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;
        private TaskCompletionSource<string> _readCompletionSource;
        private BluetoothGatt _peripheral;

        public AndroidBluetoothClientGattCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
            _readCompletionSource = new TaskCompletionSource<string>();
        }

        public override void OnConnectionStateChange(BluetoothGatt peripheral, GattStatus status, ProfileState newState)
        {
            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                // discover services offered by the peripheral we connected to
                try
                {
                    _peripheral = peripheral;
                    _peripheral.DiscoverServices();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while discovering peripheral services:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
            // ensure that all disconnected peripherals get closed (released). without closing, we'll use up all the BLE interfaces.
            else if (newState == ProfileState.Disconnected)
            {
                try
                {
                    peripheral.Close();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while closing disconnected peripheral:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt peripheral, GattStatus status)
        {
            BluetoothGattService service = null;
            try
            {
                service = peripheral.GetService(_service.Uuid);

                if (service == null)
                {
                    SensusServiceHelper.Get().Logger.Log("Null service returned. The device is not running Senus.", LoggingLevel.Normal, GetType());
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting peripheral service:  " + ex, LoggingLevel.Normal, GetType());
            }

            if (service != null)
            {
                BluetoothGattCharacteristic characteristic = null;
                try
                {
                    characteristic = service.GetCharacteristic(_characteristic.Uuid);

                    if (characteristic == null)
                    {
                        throw new Exception("Null characteristic returned.");
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while getting peripheral characteristic:  " + ex, LoggingLevel.Normal, GetType());
                }

                if (characteristic != null)
                {
                    try
                    {
                        peripheral.ReadCharacteristic(characteristic);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while reading peripheral characteristic:  " + ex, LoggingLevel.Normal, GetType());
                    }
                }
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt peripheral, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            try
            {
                byte[] valueBytes = characteristic.GetValue();
                string valueString = Encoding.UTF8.GetString(valueBytes);
                _readCompletionSource.SetResult(valueString);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting characteristic string after reading it:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        public Task<string> ReadCharacteristicValueAsync(CancellationToken cancellationToken)
        {
            return BluetoothDeviceProximityProbe.CompleteReadAsync(_readCompletionSource, cancellationToken);
        }

        public void DisconnectPeripheral()
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Disconnecting peripheral...", LoggingLevel.Normal, GetType());
                _peripheral?.Disconnect();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}

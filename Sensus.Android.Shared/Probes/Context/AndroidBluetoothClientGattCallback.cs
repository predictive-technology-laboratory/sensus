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
using System.Text;
using Sensus.Probes.Context;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Android BLE GATT client callback. Makes requests to servers to read a characteristic upon encountering a service.
    /// </summary>
    public class AndroidBluetoothClientGattCallback : BluetoothGattCallback
    {
        public event EventHandler<BluetoothCharacteristicReadArgs> CharacteristicRead;

        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;
        private DateTimeOffset _encounterTimestamp;

        public AndroidBluetoothClientGattCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic, DateTimeOffset encounterTimestamp)
        {
            _service = service;
            _characteristic = characteristic;
            _encounterTimestamp = encounterTimestamp;
        }

        public override void OnConnectionStateChange(BluetoothGatt peripheral, GattStatus status, ProfileState newState)
        {
            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                // discover services offered by the peripheral we connected to
                try
                {
                    peripheral.DiscoverServices();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while discovering peripheral services:  " + ex, LoggingLevel.Normal, GetType());
                    DisconnectPeripheral(peripheral);
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
            BluetoothGattService peripheralService;
            try
            {
                peripheralService = peripheral.GetService(_service.Uuid);

                if (peripheralService == null)
                {
                    throw new Exception("Null service returned.");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting peripheral service:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(peripheral);
                return;
            }

            BluetoothGattCharacteristic peripheralCharacteristic;
            try
            {
                peripheralCharacteristic = peripheralService.GetCharacteristic(_characteristic.Uuid);

                if (peripheralCharacteristic == null)
                {
                    throw new Exception("Null characteristic returned.");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting peripheral characteristic:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(peripheral);
                return;
            }

            try
            {
                peripheral.ReadCharacteristic(peripheralCharacteristic);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while reading peripheral characteristic:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(peripheral);
                return;
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt peripheral, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            try
            {
                byte[] characteristicBytes = characteristic.GetValue();
                string characteristicString = Encoding.UTF8.GetString(characteristicBytes);
                CharacteristicRead?.Invoke(this, new BluetoothCharacteristicReadArgs(characteristicString, _encounterTimestamp));
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting characteristic string after reading it:  " + ex, LoggingLevel.Normal, GetType());
            }
            finally
            {
                DisconnectPeripheral(peripheral);
            }
        }

        private void DisconnectPeripheral(BluetoothGatt peripheral)
        {
            try
            {
                peripheral.Disconnect();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}

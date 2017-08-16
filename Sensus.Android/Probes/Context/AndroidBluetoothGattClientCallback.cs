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
using System.Threading.Tasks;
using System.Threading;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothGattClientCallback : BluetoothGattCallback
    {
        private ManualResetEvent _connectWait = new ManualResetEvent(false);
        private GattStatus _connectResult = GattStatus.Failure;

        private ManualResetEvent _discoverWait = new ManualResetEvent(false);
        private GattStatus _discoverResult = GattStatus.Failure;

        private ManualResetEvent _readCharacteristicWait = new ManualResetEvent(false);
        private GattStatus _readCharacteristicResult = GattStatus.Failure;

        public GattStatus WaitForConnect()
        {
            _connectWait.WaitOne();
            return _connectResult;
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                _connectResult = GattStatus.Success;
                _connectWait.Set();
            }
        }

        public void DiscoverServices(BluetoothGatt gatt)
        {
            gatt.DiscoverServices();
            _discoverWait.WaitOne();
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            _discoverResult = status;
            _discoverWait.Set();
        }

        public GattStatus ReadCharacteristic(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            gatt.ReadCharacteristic(characteristic);
            _readCharacteristicWait.WaitOne();
            return _readCharacteristicResult;
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);

            _readCharacteristicResult = status;
            _readCharacteristicWait.Set();
        }
    }
}

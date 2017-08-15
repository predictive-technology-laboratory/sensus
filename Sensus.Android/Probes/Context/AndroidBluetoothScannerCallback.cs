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
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Sensus.Probes.Context;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothScannerCallback : ScanCallback
    {
        public event EventHandler<string> DeviceIdEncountered;

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
        {
            base.OnScanResult(callbackType, result);

            // result comes in on main thread. run task to release main thread.
            Task.Run(() =>
            {
                if (result == null)
                {
                    return;
                }

                // connect to peripheral
                AndroidBluetoothGattCallback gattCallback = new AndroidBluetoothGattCallback();
                BluetoothGatt peripheral = result.Device.ConnectGatt(Application.Context, false, gattCallback);
                gattCallback.WaitForConnect();

                // discover services and read device id from peripheral
                gattCallback.DiscoverServices(peripheral);
                Java.Util.UUID serviceUUID = Java.Util.UUID.FromString(BluetoothDeviceProximityProbe.SERVICE_UUID);
                BluetoothGattService service = peripheral.GetService(serviceUUID);
                Java.Util.UUID deviceIdCharacteristicUUID = Java.Util.UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_CHARACTERISTIC_UUID);
                BluetoothGattCharacteristic deviceIdCharacteristic = service.GetCharacteristic(deviceIdCharacteristicUUID);
                gattCallback.ReadCharacteristic(peripheral, deviceIdCharacteristic);
                byte[] deviceIdBytes = deviceIdCharacteristic.GetValue();
                string deviceIdEncountered = Encoding.UTF8.GetString(deviceIdBytes);
                DeviceIdEncountered?.Invoke(this, deviceIdEncountered);

                // disconnect
                peripheral.Disconnect();
            });
        }
    }
}
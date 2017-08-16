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
using Java.Util;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothScannerCallback : ScanCallback
    {
        public event EventHandler<string> DeviceIdEncountered;

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
        {
            base.OnScanResult(callbackType, result);

            if (result == null)
            {
                return;
            }

            // result comes in on main thread. run task to release main thread.
            Task.Run(() =>
            {
                try
                {
                    // connect to peripheral server
                    AndroidBluetoothGattClientCallback gattClientCallback = new AndroidBluetoothGattClientCallback();
                    BluetoothGatt peripheral = result.Device.ConnectGatt(Application.Context, false, gattClientCallback);
                    gattClientCallback.WaitForConnect();

                    // discover services and read device id from peripheral
                    gattClientCallback.DiscoverServices(peripheral);
                    UUID serviceUUID = UUID.FromString(BluetoothDeviceProximityProbe.SERVICE_UUID);
                    BluetoothGattService service = peripheral.GetService(serviceUUID);
                    UUID deviceIdCharacteristicUUID = UUID.FromString(BluetoothDeviceProximityProbe.DEVICE_ID_CHARACTERISTIC_UUID);
                    BluetoothGattCharacteristic deviceIdCharacteristic = service.GetCharacteristic(deviceIdCharacteristicUUID);
                    gattClientCallback.ReadCharacteristic(peripheral, deviceIdCharacteristic);
                    byte[] deviceIdBytes = deviceIdCharacteristic.GetValue();
                    string deviceIdEncountered = Encoding.UTF8.GetString(deviceIdBytes);
                    DeviceIdEncountered?.Invoke(this, deviceIdEncountered);

                    // disconnect
                    peripheral.Disconnect();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading device ID from peripheral:  " + ex, LoggingLevel.Normal, GetType());
                }
            });
        }
    }
}
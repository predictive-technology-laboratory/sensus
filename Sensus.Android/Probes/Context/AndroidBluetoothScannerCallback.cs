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
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;

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

            try
            {
                AndroidBluetoothGattClientCallback gattClientCallback = new AndroidBluetoothGattClientCallback();

                if (DeviceIdEncountered != null)
                {
                    gattClientCallback.DeviceIdEncountered += DeviceIdEncountered;
                }

                // connect as gatt client to read data from peripheral server
                result.Device.ConnectGatt(Application.Context, false, gattClientCallback);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while reading device ID from peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}
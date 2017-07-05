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
using Sensus.Probes.Context;
using Plugin.Permissions.Abstractions;
using Android.Bluetooth.LE;
using Android.Bluetooth;
using Sensus.Context;
using Android.OS;
using Java.Util;
using System.Text;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private BluetoothLeScanner _bluetoothScanner;
        private AndroidBluetoothScanCallback _bluetoothScannerCallback;
        private BluetoothLeAdvertiser _bluetoothAdvertiser;
        private AndroidBluetoothAdvertisingCallback _bluetoothAdvertiserCallback;

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void StartListening()
        {
            // adapted primarily from:  https://code.tutsplus.com/tutorials/how-to-advertise-android-as-a-bluetooth-le-peripheral--cms-25426

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                ParcelUuid serviceUUID = new ParcelUuid(UUID.FromString(SERVICE_UUID));

                #region central -- scan for the sensus BLE probe peripheral
                ScanFilter scanFilter = new ScanFilter.Builder()
                                                      .SetServiceUuid(serviceUUID)
                                                      .Build();

                List<ScanFilter> scanFilters = new List<ScanFilter>();
                scanFilters.Add(scanFilter);

                ScanSettings scanSettings = new ScanSettings.Builder()
                                                            .SetScanMode(global::Android.Bluetooth.LE.ScanMode.Balanced)
                                                            .Build();

                _bluetoothScannerCallback = new AndroidBluetoothScanCallback();

                _bluetoothScannerCallback.DeviceIdEncountered += async (sender, deviceIdEncountered) =>
                {
                    await StoreDatumAsync(new BluetoothDeviceProximityDatum(DateTimeOffset.UtcNow, deviceIdEncountered));
                };

                _bluetoothScanner = BluetoothAdapter.DefaultAdapter.BluetoothLeScanner;
                _bluetoothScanner.StartScan(scanFilters, scanSettings, _bluetoothScannerCallback);
                #endregion

                #region peripheral -- advertise the sensus BLE probe peripheral. not supported by all hardware models.
                if (BluetoothAdapter.DefaultAdapter.IsMultipleAdvertisementSupported)
                {
                    AdvertiseSettings advertiseSettings = new AdvertiseSettings.Builder()
                                                                               .SetAdvertiseMode(AdvertiseMode.Balanced)
                                                                               .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                                                                               .SetConnectable(true)
                                                                               .Build();

                    AdvertiseData advertiseData = new AdvertiseData.Builder()
                                                                   .SetIncludeDeviceName(false)
                                                                   .AddServiceUuid(serviceUUID)
                                                                   .Build();

                    _bluetoothAdvertiserCallback = new AndroidBluetoothAdvertisingCallback();

                    _bluetoothAdvertiser = BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser;
                    _bluetoothAdvertiser.StartAdvertising(advertiseSettings, advertiseData, _bluetoothAdvertiserCallback);

                    // how do we handle read requests from the client?
                }
                #endregion
            });
        }

        protected override void StopListening()
        {
            _bluetoothScanner.StopScan(_bluetoothScannerCallback);
            _bluetoothAdvertiser.StopAdvertising(_bluetoothAdvertiserCallback);
        }
    }
}
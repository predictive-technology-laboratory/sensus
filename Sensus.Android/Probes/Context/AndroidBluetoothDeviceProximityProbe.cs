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
using Android.OS;
using Java.Util;
using System.Collections.Generic;
using Android.App;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private AndroidBluetoothScannerCallback _bluetoothScannerCallback;
        private AndroidBluetoothAdvertisingCallback _bluetoothAdvertiserCallback;
        private BluetoothGattServer _bluetoothGattServer;
        private BluetoothGattService _gattService;

        protected override void Initialize()
        {
            base.Initialize();

            // BLE requires location permissions
            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void StartCentral()
        {
            ParcelUuid serviceUUID = new ParcelUuid(UUID.FromString(DEVICE_ID_SERVICE_UUID));

            ScanFilter scanFilter = new ScanFilter.Builder()
                                                  .SetServiceUuid(serviceUUID)
                                                  .Build();

            List<ScanFilter> scanFilters = new List<ScanFilter>();
            scanFilters.Add(scanFilter);

            ScanSettings scanSettings = new ScanSettings.Builder()
                                                        .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowPower)
                                                        .Build();

            _bluetoothScannerCallback = new AndroidBluetoothScannerCallback();
            _bluetoothScannerCallback.DeviceIdEncountered += async (sender, deviceIdEncountered) =>
            {
                await StoreDatumAsync(new BluetoothDeviceProximityDatum(DateTimeOffset.UtcNow, deviceIdEncountered));
            };

            BluetoothAdapter.DefaultAdapter.BluetoothLeScanner.StartScan(scanFilters, scanSettings, _bluetoothScannerCallback);
        }

        protected override void StartPeripheral()
        {
            if (BluetoothAdapter.DefaultAdapter.IsMultipleAdvertisementSupported)
            {
                UUID serviceUUID = UUID.FromString(DEVICE_ID_SERVICE_UUID);

                // open server with service/characteristic
                BluetoothGattCharacteristic gattCharacteristic = new BluetoothGattCharacteristic(UUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID),
                                                                                                 GattProperty.Read,
                                                                                                 GattPermission.Read);

                _gattService = new BluetoothGattService(serviceUUID, GattServiceType.Primary);
                _gattService.AddCharacteristic(gattCharacteristic);

                // open gatt server
                BluetoothManager bluetoothManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as BluetoothManager;
                AndroidBluetoothGattServerCallback serverCallback = new AndroidBluetoothGattServerCallback();
                _bluetoothGattServer = bluetoothManager.OpenGattServer(Application.Context, serverCallback);

                // add service 
                _bluetoothGattServer.AddService(_gattService);

                // set server on callback for responding to requests
                serverCallback.Server = _bluetoothGattServer;

                // start advertisement
                AdvertiseSettings advertiseSettings = new AdvertiseSettings.Builder()
                                                                           .SetAdvertiseMode(AdvertiseMode.Balanced)
                                                                           .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                                                                           .SetConnectable(true)
                                                                           .Build();

                AdvertiseData advertiseData = new AdvertiseData.Builder()
                                                               .SetIncludeDeviceName(false)
                                                               .AddServiceUuid(new ParcelUuid(serviceUUID))
                                                               .Build();

                _bluetoothAdvertiserCallback = new AndroidBluetoothAdvertisingCallback();
                BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser.StartAdvertising(advertiseSettings, advertiseData, _bluetoothAdvertiserCallback);
            }
            else
            {
                throw new Exception("BLE advertising is not available.");
            }
        }

        protected override void StopCentral()
        {
            // stop scanning
            try
            {
                BluetoothAdapter.DefaultAdapter.BluetoothLeScanner?.StopScan(_bluetoothScannerCallback);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _bluetoothScannerCallback = null;
            }
        }

        protected override void StopPeripheral()
        {
            // stop advertising
            try
            {
                BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser?.StopAdvertising(_bluetoothAdvertiserCallback);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping advertiser:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _bluetoothAdvertiserCallback = null;
            }

            // remove the service
            try
            {
                _bluetoothGattServer?.RemoveService(_gattService);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing service:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _gattService = null;
            }

            // close the server
            try
            {
                _bluetoothGattServer?.Close();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while closing GATT server:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _bluetoothGattServer = null;
            }
        }
    }
}
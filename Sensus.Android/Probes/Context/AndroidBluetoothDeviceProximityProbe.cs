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
using Newtonsoft.Json;
using System.Text;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private AndroidBluetoothScannerCallback _bluetoothScannerCallback;
        private AndroidBluetoothAdvertisingCallback _bluetoothAdvertiserCallback;
        private BluetoothGattServer _bluetoothGattServer;
        private BluetoothGattService _deviceIdService;
        private BluetoothGattCharacteristic _deviceIdCharacteristic;

        [JsonIgnore]
        public BluetoothGattService DeviceIdService
        {
            get
            {
                return _deviceIdService;
            }
        }

        [JsonIgnore]
        public BluetoothGattCharacteristic DeviceIdCharacteristic
        {
            get
            {
                return _deviceIdCharacteristic;
            }
        }

        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        public AndroidBluetoothDeviceProximityProbe()
        {
            _bluetoothScannerCallback = new AndroidBluetoothScannerCallback(this);

            _bluetoothScannerCallback.DeviceIdEncountered += (sender, bluetoothDeviceProximityDatum) =>
            {
                lock (EncounteredDeviceData)
                {
                    EncounteredDeviceData.Add(bluetoothDeviceProximityDatum);
                }
            };
        }

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

            _deviceIdCharacteristic = new BluetoothGattCharacteristic(UUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID),
                                                                                             GattProperty.Read,
                                                                                             GattPermission.Read);

            _deviceIdCharacteristic.SetValue(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId));

            _deviceIdService = new BluetoothGattService(UUID.FromString(DEVICE_ID_SERVICE_UUID), GattServiceType.Primary);
            _deviceIdService.AddCharacteristic(_deviceIdCharacteristic);
        }

        #region central -- scan
        protected override void StartScan()
        {
            ScanFilter scanFilter = new ScanFilter.Builder()
                                                  .SetServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
                                                  .Build();

            List<ScanFilter> scanFilters = new List<ScanFilter>(new[] { scanFilter });

            ScanSettings.Builder scanSettingsBuilder = new ScanSettings.Builder()
                                                                       .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowPower);

            // batch scan results if supported on the BLE chip
            if (BluetoothAdapter.DefaultAdapter.IsOffloadedScanBatchingSupported)
            {
                scanSettingsBuilder.SetReportDelay((long)TimeSpan.FromSeconds(30).TotalMilliseconds);
            }

            BluetoothAdapter.DefaultAdapter.BluetoothLeScanner.StartScan(scanFilters, scanSettingsBuilder.Build(), _bluetoothScannerCallback);
        }

        protected override void StopScan()
        {
            // stop scanning
            try
            {
                BluetoothAdapter.DefaultAdapter.BluetoothLeScanner.StopScan(_bluetoothScannerCallback);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }
        #endregion

        #region peripheral -- advertise
        protected override void StartAdvertising()
        {
            if (BluetoothAdapter.DefaultAdapter.IsMultipleAdvertisementSupported)
            {
                // open gatt server
                BluetoothManager bluetoothManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as BluetoothManager;
                AndroidBluetoothGattServerCallback serverCallback = new AndroidBluetoothGattServerCallback(this);
                _bluetoothGattServer = bluetoothManager.OpenGattServer(Application.Context, serverCallback);

                // set server on callback for responding to requests
                serverCallback.Server = _bluetoothGattServer;

                // add service 
                _bluetoothGattServer.AddService(_deviceIdService);

                // start advertisement
                AdvertiseSettings advertiseSettings = new AdvertiseSettings.Builder()
                                                                           .SetAdvertiseMode(AdvertiseMode.Balanced)
                                                                           .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                                                                           .SetConnectable(true)
                                                                           .Build();

                AdvertiseData advertiseData = new AdvertiseData.Builder()
                                                               .SetIncludeDeviceName(false)
                                                               .AddServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
                                                               .Build();

                _bluetoothAdvertiserCallback = new AndroidBluetoothAdvertisingCallback();

                BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser.StartAdvertising(advertiseSettings, advertiseData, _bluetoothAdvertiserCallback);
            }
            else
            {
                throw new Exception("BLE advertising is not available.");
            }
        }

        protected override void StopAdvertising()
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
                _bluetoothGattServer?.RemoveService(_deviceIdService);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing service:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _deviceIdService = null;
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
        #endregion
    }
}
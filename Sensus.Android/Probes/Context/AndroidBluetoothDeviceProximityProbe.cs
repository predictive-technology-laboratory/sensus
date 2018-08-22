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
using System.Text;
using Sensus.Context;
using System.Threading;
using Newtonsoft.Json;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Scans for the presence of other devices nearby that are running the current <see cref="Protocol"/>. When
    /// encountered, will read the device ID of these other devices. Also advertises the presence of the current
    /// device and serves requests for the current device's ID.
    /// </summary>
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private AndroidBluetoothClientScannerCallback _bluetoothScannerCallback;
        private AndroidBluetoothServerAdvertisingCallback _bluetoothAdvertiserCallback;
        private BluetoothGattService _deviceIdService;

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

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

            BluetoothGattCharacteristic deviceIdCharacteristic = new BluetoothGattCharacteristic(UUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID), GattProperty.Read, GattPermission.Read);
            deviceIdCharacteristic.SetValue(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId));

            _deviceIdService = new BluetoothGattService(UUID.FromString(Protocol.Id), GattServiceType.Primary);
            _deviceIdService.AddCharacteristic(deviceIdCharacteristic);

            _bluetoothScannerCallback = new AndroidBluetoothClientScannerCallback(_deviceIdService, deviceIdCharacteristic);

            // add any read characteristics to the collection
            _bluetoothScannerCallback.CharacteristicRead += (sender, e) =>
            {
                lock (EncounteredDeviceData)
                {
                    EncounteredDeviceData.Add(new BluetoothDeviceProximityDatum(e.Timestamp, e.Value));
                }
            };

            _bluetoothAdvertiserCallback = new AndroidBluetoothServerAdvertisingCallback(_deviceIdService, deviceIdCharacteristic);
        }

        #region central -- scan
        protected override void StartScan()
        {
            TimeSpan? reportDelay = null;

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // start a scan if bluetooth is present and enabled
                if (BluetoothAdapter.DefaultAdapter?.IsEnabled ?? false)
                {
                    try
                    {
                        ScanFilter scanFilter = new ScanFilter.Builder()
                                                              .SetServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
                                                              .Build();

                        List<ScanFilter> scanFilters = new List<ScanFilter>(new[] { scanFilter });

                        ScanSettings.Builder scanSettingsBuilder = new ScanSettings.Builder()
                                                                                   .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowPower);

                        // return batched scan results periodically if supported on the BLE chip
                        if (BluetoothAdapter.DefaultAdapter.IsOffloadedScanBatchingSupported)
                        {
                            reportDelay = TimeSpan.FromSeconds(10);
                            scanSettingsBuilder.SetReportDelay((long)reportDelay.Value.TotalMilliseconds);
                        }

                        BluetoothAdapter.DefaultAdapter.BluetoothLeScanner.StartScan(scanFilters, scanSettingsBuilder.Build(), _bluetoothScannerCallback);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while starting scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }
            });

            // if we're batching, wait twice the report delay for some results to come in. we sleep below so as not to return from the poll
            // and release the wakelock we're currently holding.
            if (reportDelay != null)
            {
                SensusServiceHelper.Get().AssertNotOnMainThread("Waiting for BLE scan results.");
                Thread.Sleep((int)(reportDelay.Value.TotalMilliseconds * 2));

                lock (EncounteredDeviceData)
                {
                    SensusServiceHelper.Get().Logger.Log("Encountered " + EncounteredDeviceData.Count + " device(s).", LoggingLevel.Normal, GetType());
                }
            }

            // we've scanned and waited for results to come in. stop scanning and wait for next poll.
            StopScan();
        }

        protected override void StopScan()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    BluetoothAdapter.DefaultAdapter?.BluetoothLeScanner.StopScan(_bluetoothScannerCallback);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }
        #endregion

        #region peripheral -- advertise
        protected override void StartAdvertising()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    if (BluetoothAdapter.DefaultAdapter?.IsMultipleAdvertisementSupported ?? false)
                    {
                        AdvertiseSettings advertiseSettings = new AdvertiseSettings.Builder()
                                                                                   .SetAdvertiseMode(AdvertiseMode.LowPower)
                                                                                   .SetTxPowerLevel(AdvertiseTx.PowerLow)
                                                                                   .SetConnectable(true)
                                                                                   .Build();

                        AdvertiseData advertiseData = new AdvertiseData.Builder()
                                                                       .SetIncludeDeviceName(false)
                                                                       .AddServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
                                                                       .Build();

                        BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser.StartAdvertising(advertiseSettings, advertiseData, _bluetoothAdvertiserCallback);
                    }
                    else
                    {
                        throw new Exception("BLE advertising is not available.");
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting advertiser:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        protected override void StopAdvertising()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // stop advertising
                try
                {
                    BluetoothAdapter.DefaultAdapter?.BluetoothLeAdvertiser.StopAdvertising(_bluetoothAdvertiserCallback);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping advertiser:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                _bluetoothAdvertiserCallback.CloseServer();
            });
        }

        public override bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = base.TestHealth(ref events);

            if (Running)
            {
                // if the user disables/enables BT manually, we will no longer be advertising the service. start advertising
                // on each health test to ensure we're advertising.
                StartAdvertising();
            }

            return restart;
        }
        #endregion
    }
}
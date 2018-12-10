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
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Scans for the presence of other devices nearby that are running the current <see cref="Protocol"/>. When
    /// encountered, this Probe will read the device ID of other devices. This Probe also advertises the presence 
    /// of the current device and serves requests for the current device's ID. This Probe reports data in the form 
    /// of <see cref="BluetoothDeviceProximityDatum"/> objects.
    /// 
    /// There are caveats to the conditions under which an Android device running this Probe will detect another
    /// device:
    /// 
    ///   * If the other device is an Android device with Sensus running in the foreground or background, detection is possible.
    ///   * If the other device is an iOS device with Sensus running in the foreground, detection is possible.
    ///   * If the other device is an iOS device with Sensus running in the background, detection is not possible.
    /// 
    /// NOTE:  The value of <see cref="Protocol.Id"/> running on the other device must equal the value of 
    /// <see cref="Protocol.Id"/> running on the current device. When a Protocol is created from within the
    /// Sensus app, it is assigned a unique identifier. This value is maintained or changed depending on what you
    /// do:
    /// 
    ///   * When the newly created Protocol is copied on the current device, a new unique identifier is assigned to
    ///     it. This breaks the connection between the Protocols.
    /// 
    ///   * When the newly created Protocol is shared via the app with another device, its identifier remains 
    ///     unchanged. This maintains the connection between the Protocols.
    /// 
    /// Thus, in order for this <see cref="AndroidBluetoothDeviceProximityProbe"/> to operate properly, you must configure
    /// your Protocols in one of the two following ways:
    /// 
    ///   * Create your Protocol on one platform (either Android or iOS) and then share it with a device from the other
    ///     platform for customization. The <see cref="Protocol.Id"/> values of these Protocols will remain equal
    ///     and this <see cref="AndroidBluetoothDeviceProximityProbe"/> will detect encounters across platforms.
    /// 
    ///   * Create your Protocols separately on each platform and then set the <see cref="Protocol.Id"/> field on
    ///     one platform (using the "Set Study Identifier" button) to match the <see cref="Protocol.Id"/> value
    ///     of the other platform (obtained via "Copy Study Identifier").
    /// 
    /// See the iOS subclass of <see cref="BluetoothDeviceProximityProbe"/> for additional information.
    /// </summary>
    public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private AndroidBluetoothClientScannerCallback _bluetoothScannerCallback;
        private AndroidBluetoothServerAdvertisingCallback _bluetoothAdvertiserCallback;
        private BluetoothGattService _deviceIdService;

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // BLE requires location permissions
            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Bluetooth probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
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
                                                                                   .SetScanMode(global::Android.Bluetooth.LE.ScanMode.Balanced);

                        // return batched scan results periodically if supported on the BLE chip
                        if (BluetoothAdapter.DefaultAdapter.IsOffloadedScanBatchingSupported)
                        {
                            scanSettingsBuilder.SetReportDelay((long)(ScanDurationMS / 2.0));
                        }

                        BluetoothAdapter.DefaultAdapter.BluetoothLeScanner.StartScan(scanFilters, scanSettingsBuilder.Build(), _bluetoothScannerCallback);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while starting scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }
            });
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
                        AdvertiseSettings advertisingSettings = new AdvertiseSettings.Builder()
                                                                                     .SetAdvertiseMode(AdvertiseMode.Balanced)
                                                                                     .SetTxPowerLevel(AdvertiseTx.PowerLow)
                                                                                     .SetConnectable(true)
                                                                                     .Build();

                        AdvertiseData advertisingData = new AdvertiseData.Builder()
                                                                         .SetIncludeDeviceName(false)
                                                                         .AddServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
                                                                         .Build();

                        BluetoothAdapter.DefaultAdapter.BluetoothLeAdvertiser.StartAdvertising(advertisingSettings, advertisingData, _bluetoothAdvertiserCallback);
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
                try
                {
                    BluetoothAdapter.DefaultAdapter?.BluetoothLeAdvertiser.StopAdvertising(_bluetoothAdvertiserCallback);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                _bluetoothAdvertiserCallback.CloseServer();
            });
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            if (Running)
            {
                // if the user disables/enables BT manually, we will no longer be advertising the service. start advertising
                // on each health test to ensure we're advertising.
                StartAdvertising();
            }

            return result;
        }
        #endregion
    }
}

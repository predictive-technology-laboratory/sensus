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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using CoreFoundation;
using Foundation;
using Newtonsoft.Json;
using Sensus.Probes.Context;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    /// Scans for the presence of other devices nearby that are running the current <see cref="Protocol"/>. When
    /// encountered, this Probe will read the device ID of other devices. This Probe also advertises the presence 
    /// of the current device and serves requests for the current device's ID. This Probe reports data in the form 
    /// of <see cref="BluetoothDeviceProximityDatum"/> objects. There are no caveats to the conditions under which 
    /// an iOS device running this Probe will detect another device. Detection is possible if the other device is
    /// Android or iOS and if Sensus is foregrounded or backgrounded on the other device.
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
    /// Thus, in order for this <see cref="iOSBluetoothDeviceProximityProbe"/> to operate properly, you must configure
    /// your Protocols in one of the two following ways:
    /// 
    ///   * Create your Protocol on one platform (either Android or iOS) and then share it with a device from the other
    ///     platform for customization. The <see cref="Protocol.Id"/> values of these Protocols will remain equal
    ///     and this <see cref="iOSBluetoothDeviceProximityProbe"/> will detect encounters across platforms.
    /// 
    ///   * Create your Protocols separately on each platform and then set the <see cref="Protocol.Id"/> field on
    ///     one platform (using the "Set Study Identifier" button) to match the <see cref="Protocol.Id"/> value
    ///     of the other platform (obtained via "Copy Study Identifier").
    /// 
    /// See the Android subclass of <see cref="BluetoothDeviceProximityProbe"/> for additional information.
    /// </summary>
    public class iOSBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private CBMutableService _deviceIdService;
        private CBMutableCharacteristic _deviceIdCharacteristic;
        private CBCentralManager _bluetoothCentralManager;
        private iOSBluetoothDeviceProximityProbeCentralManagerDelegate _bluetoothCentralManagerDelegate;
        private CBPeripheralManager _bluetoothPeripheralManager;

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // create device id characteristic
            _deviceIdCharacteristic = new CBMutableCharacteristic(CBUUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID),
                                                                  CBCharacteristicProperties.Read,
                                                                  NSData.FromArray(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId)),
                                                                  CBAttributePermissions.Readable);

            // create service with device id characteristic
            _deviceIdService = new CBMutableService(CBUUID.FromString(Protocol.Id), true);
            _deviceIdService.Characteristics = new CBCharacteristic[] { _deviceIdCharacteristic };
        }

        #region central -- scan
        protected override async Task ScanAsync(CancellationToken cancellationToken)
        {
            try
            {
                // start a fresh manager delegate to collect/read results
                _bluetoothCentralManagerDelegate = new iOSBluetoothDeviceProximityProbeCentralManagerDelegate(_deviceIdService, _deviceIdCharacteristic, this);

                // initialize manager, which starts scan.
                _bluetoothCentralManager = new CBCentralManager(_bluetoothCentralManagerDelegate,
                                                                new DispatchQueue("ble"),
                                                                NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBCentralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.

                TaskCompletionSource<bool> scanCompletionSource = new TaskCompletionSource<bool>();

                cancellationToken.Register(() =>
                {
                    try
                    {
                        _bluetoothCentralManager.StopScan();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while stopping scan:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                    finally
                    {
                        _bluetoothCentralManager = null;
                        scanCompletionSource.TrySetResult(true);
                    }
                });

                await scanCompletionSource.Task;
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while scanning:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        protected override Task<List<BluetoothDeviceProximityDatum>> ReadPeripheralCharacteristicValuesAsync(CancellationToken cancellationToken)
        {
            return _bluetoothCentralManagerDelegate.ReadPeripheralCharacteristicValuesAsync(cancellationToken);
        }
        #endregion

        #region peripheral -- advertise
        protected override void StartAdvertising()
        {
            try
            {
                _bluetoothPeripheralManager = new CBPeripheralManager(new iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(_deviceIdService, _deviceIdCharacteristic, this),
                                                                      new DispatchQueue("ble"),
                                                                      NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBPeripheralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while starting advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        protected override void StopAdvertising()
        {
            // remove service
            try
            {
                SensusServiceHelper.Get().Logger.Log("Removing service.", LoggingLevel.Normal, GetType());
                _bluetoothPeripheralManager?.RemoveService(_deviceIdService);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            // stop advertising
            try
            {
                SensusServiceHelper.Get().Logger.Log("Stopping peripheral advertising.", LoggingLevel.Normal, GetType());
                _bluetoothPeripheralManager?.StopAdvertising();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
            finally
            {
                _bluetoothPeripheralManager = null;
            }
        }
        #endregion
    }
}
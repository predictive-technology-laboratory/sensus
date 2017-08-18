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
using Syncfusion.SfChart.XForms;
using CoreBluetooth;
using CoreFoundation;
using Sensus.Context;
using Foundation;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Sensus.iOS.Probes.Context
{
    public class iOSBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private CBCentralManager _bluetoothCentralManager;
        private CBPeripheralManager _bluetoothPeripheralManager;
        private CBMutableCharacteristic _deviceIdCharacteristic;
        private CBMutableService _deviceIdService;

        [JsonIgnore]
        public CBMutableCharacteristic DeviceIdCharacteristic
        {
            get
            {
                return _deviceIdCharacteristic;
            }
        }

        [JsonIgnore]
        public CBMutableService DeviceIdService
        {
            get
            {
                return _deviceIdService;
            }
        }

        protected override void Initialize()
        {
            // create device id characteristic
            _deviceIdCharacteristic = new CBMutableCharacteristic(CBUUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID),
                                                                  CBCharacteristicProperties.Read,
                                                                  NSData.FromArray(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId)),
                                                                  CBAttributePermissions.Readable);

            // create service with device id characteristic
            _deviceIdService = new CBMutableService(CBUUID.FromString(DEVICE_ID_SERVICE_UUID), true);
            _deviceIdService.Characteristics = new CBCharacteristic[] { _deviceIdCharacteristic };
        }

        #region central
        protected override void StartCentral()
        {
            if (_bluetoothCentralManager == null)
            {
                _bluetoothCentralManager = new CBCentralManager(DispatchQueue.MainQueue);

                // the central manager may be powered on/off by the user after the probe has been started. cover the case where this happens,
                // and start scanning if the new central manager state is powered on and the probe is running.
                _bluetoothCentralManager.UpdatedState += (sender, e) =>
                {
                    if (_bluetoothCentralManager.State == CBCentralManagerState.PoweredOn && Running)
                    {
                        StartScanning();
                    }
                };

                // if we discover a sensus peripheral, read and store its device id
                _bluetoothCentralManager.DiscoveredPeripheral += async (sender, e) =>
                {
                    try
                    {
                        _bluetoothCentralManager.ConnectPeripheral(e.Peripheral);

                        if (e.Peripheral.IsConnected)
                        {
                            CBMutableCharacteristic deviceIdRead = new CBMutableCharacteristic(_deviceIdCharacteristic.UUID, 
                                                                                               _deviceIdCharacteristic.Properties, 
                                                                                               null, 
                                                                                               _deviceIdCharacteristic.Permissions);
                            e.Peripheral.ReadValue(deviceIdRead);
                            string encounteredDeviceId = Encoding.UTF8.GetString(deviceIdRead.Value.ToArray());
                            await StoreDatumAsync(new BluetoothDeviceProximityDatum(DateTime.UtcNow, encounteredDeviceId));
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to read device ID characteristic from Sensus BLE peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                };

                _bluetoothCentralManager.ConnectedPeripheral += (sender, e) =>
                {
                    SensusServiceHelper.Get().Logger.Log("Connected peripheral.", LoggingLevel.Normal, GetType());
                };

                _bluetoothCentralManager.FailedToConnectPeripheral += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to connect peripheral:  " + e.Error, LoggingLevel.Normal, GetType());
                    }
                };
            }
            else
            {
                StartScanning();
            }
        }

        private void StartScanning()
        {
            try
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    if (!_bluetoothCentralManager.IsScanning)
                    {
                        _bluetoothCentralManager.ScanForPeripherals(_deviceIdService.UUID);
                    }
                });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while starting scan for service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        protected override void StopCentral()
        {
            if (_bluetoothCentralManager.IsScanning)
            {
                try
                {
                    _bluetoothCentralManager.StopScan();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping scanning for service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }
        #endregion

        #region peripheral
        protected override void StartPeripheral()
        {
            _bluetoothPeripheralManager = new CBPeripheralManager(new iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(this),
                                                                  DispatchQueue.MainQueue,
                                                                  NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(true), CBPeripheralManager.OptionShowPowerAlertKey));
        }

        protected override void StopPeripheral()
        {
            try
            {
                _bluetoothPeripheralManager.RemoveService(_deviceIdService);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            try
            {
                _bluetoothPeripheralManager.StopAdvertising();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }
        #endregion
    }
}

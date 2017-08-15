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

namespace Sensus.iOS.Probes.Context
{
    public class iOSBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private CBCentralManager _bluetoothCentralManager;
        private CBPeripheralManager _bluetoothPeripheralManager;
        private CBUUID _deviceIdUUID;
        private CBCharacteristicProperties _deviceIdProperties;
        private NSData _deviceIdValue;
        private CBAttributePermissions _deviceIdPermissions;
        private CBMutableCharacteristic _deviceId;
        private CBUUID _serviceUUID;
        private CBMutableService _service;

        protected override void Initialize()
        {
            base.Initialize();

            // create device id characteristic
            _deviceIdUUID = CBUUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID);
            _deviceIdProperties = CBCharacteristicProperties.Read;
            _deviceIdValue = NSData.FromArray(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId));
            _deviceIdPermissions = CBAttributePermissions.Readable;
            _deviceId = new CBMutableCharacteristic(_deviceIdUUID, _deviceIdProperties, _deviceIdValue, _deviceIdPermissions);

            // create service with device id characteristic
            _serviceUUID = CBUUID.FromString(SERVICE_UUID);
            _service = new CBMutableService(_serviceUUID, true);
            _service.Characteristics = new CBCharacteristic[] { _deviceId };

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                #region sensus central -- scan for the sensus BLE probe peripheral
                _bluetoothCentralManager = new CBCentralManager(DispatchQueue.MainQueue);

                // the central manager may be powered on/off by the user after the probe has been started. cover the case where this happens,
                // and start scanning if the new central manager state is powered on, we're not scanning, and the probe is running.
                _bluetoothCentralManager.UpdatedState += (sender, e) =>
                {
                    if (_bluetoothCentralManager.State == CBCentralManagerState.PoweredOn && !_bluetoothCentralManager.IsScanning && Running)
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
                            CBMutableCharacteristic deviceIdRead = new CBMutableCharacteristic(_deviceIdUUID, _deviceIdProperties, null, _deviceIdPermissions);
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
                #endregion

                #region sensus peripheral -- advertise the sensus BLE probe peripheral
                _bluetoothPeripheralManager = new CBPeripheralManager();

                // the peripheral manager may be powered on/off by the user after the probe has been started. cover the case where this happens,
                // and start advertising if the new peripheral manager state is powered on, we're not advertising, and the probe is running.
                _bluetoothPeripheralManager.StateUpdated += (sender, e) =>
                {
                    if (_bluetoothPeripheralManager.State == CBPeripheralManagerState.PoweredOn && !_bluetoothPeripheralManager.Advertising && Running)
                    {
                        StartAdvertising();
                    }
                };

                _bluetoothPeripheralManager.ServiceAdded += (sender, e) =>
                {
                    if (e.Error == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Added service.", LoggingLevel.Normal, GetType());
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("Error adding service:  " + e.Error, LoggingLevel.Normal, GetType());
                    }
                };

                _bluetoothPeripheralManager.AdvertisingStarted += (sender, e) =>
                {
                    if (e.Error == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Advertising started.", LoggingLevel.Normal, GetType());
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("Error starting advertising:  " + e.Error, LoggingLevel.Normal, GetType());
                    }
                };

                // service any read requests from centrals that are for our device id characteristic
                _bluetoothPeripheralManager.ReadRequestReceived += (sender, e) =>
                {
                    try
                    {
                        CBATTRequest request = e.Request;

                        if (!request.Characteristic.UUID.Equals(_deviceIdUUID))
                        {
                            _bluetoothPeripheralManager.RespondToRequest(request, CBATTError.RequestNotSupported);
                        }
                        else if (request.Offset > (nint)_deviceIdValue.Length)
                        {
                            _bluetoothPeripheralManager.RespondToRequest(request, CBATTError.InvalidOffset);
                        }
                        else
                        {
                            // fill in the device id value for the request and return it to the central
                            request.Value = _deviceIdValue;
                            _bluetoothPeripheralManager.RespondToRequest(request, CBATTError.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to service central read request:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                };
                #endregion
            });
        }

        protected override void StartListening()
        {
            base.StartListening();

            if (!_bluetoothCentralManager.IsScanning)
            {
                StartScanning();
            }

            if (!_bluetoothPeripheralManager.Advertising)
            {
                StartAdvertising();
            }
        }

        private void StartScanning()
        {
            try
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    _bluetoothCentralManager.ScanForPeripherals(_serviceUUID);
                });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while starting scan for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        private void StartAdvertising()
        {
            try
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    _bluetoothPeripheralManager.AddService(_service);
                    NSDictionary advertisements = new NSDictionary(CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(_serviceUUID));
                    _bluetoothPeripheralManager.StartAdvertising(advertisements);
                });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while starting advertising for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        protected override void StopListening()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                #region stop central
                if (_bluetoothCentralManager?.IsScanning ?? false)
                {
                    try
                    {
                        _bluetoothCentralManager.StopScan();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while stopping scanning for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                _bluetoothCentralManager = null;
                #endregion

                #region peripheral
                if (_bluetoothPeripheralManager?.Advertising ?? false)
                {
                    try
                    {
                        _bluetoothPeripheralManager?.StopAdvertising();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                try
                {
                    _bluetoothPeripheralManager?.RemoveService(_service);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while removing service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                _bluetoothPeripheralManager = null;
                #endregion
            });
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return null;
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
        }
    }
}

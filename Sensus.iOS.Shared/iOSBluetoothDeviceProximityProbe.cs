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

            _deviceIdUUID = CBUUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID);
            _deviceIdProperties = CBCharacteristicProperties.Read;
            _deviceIdValue = NSData.FromArray(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId));
            _deviceIdPermissions = CBAttributePermissions.Readable;
            _deviceId = new CBMutableCharacteristic(_deviceIdUUID, _deviceIdProperties, _deviceIdValue, _deviceIdPermissions);

            _serviceUUID = CBUUID.FromString(SERVICE_UUID);
            _service = new CBMutableService(_serviceUUID, true);
            _service.Characteristics = new CBCharacteristic[] { _deviceId };

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                #region central -- scan for the sensus BLE probe peripheral
                _bluetoothCentralManager = new CBCentralManager(DispatchQueue.MainQueue);

                _bluetoothCentralManager.UpdatedState += (sender, e) =>
                {
                    if (_bluetoothCentralManager.State == CBCentralManagerState.PoweredOn && !_bluetoothCentralManager.IsScanning && Running)
                    {
                        StartScanning();
                    }
                };

                _bluetoothCentralManager.DiscoveredPeripheral += async (sender, e) =>
                {
                    _bluetoothCentralManager.ConnectPeripheral(e.Peripheral);

                    if (e.Peripheral.IsConnected)
                    {
                        CBMutableCharacteristic readDeviceCharacteristic = new CBMutableCharacteristic(_deviceIdUUID, _deviceIdProperties, null, _deviceIdPermissions);
                        e.Peripheral.ReadValue(readDeviceCharacteristic);
                        string encounteredDeviceId = Encoding.UTF8.GetString(readDeviceCharacteristic.Value.ToArray());
                        await StoreDatumAsync(new BluetoothDeviceProximityDatum(DateTime.UtcNow, encounteredDeviceId));
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

                #region peripheral -- advertise the sensus BLE probe peripheral
                _bluetoothPeripheralManager = new CBPeripheralManager();

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

                _bluetoothPeripheralManager.ReadRequestReceived += (sender, e) =>
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
                        request.Value = _deviceIdValue;
                        _bluetoothPeripheralManager.RespondToRequest(request, CBATTError.Success);
                    }
                };
                #endregion
            });
        }

        protected override void StartListening()
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth, which is being used in one of your studies."))
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Bluetooth not enabled. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

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
                if (_bluetoothCentralManager?.IsScanning ?? false)
                {
                    try
                    {
                        _bluetoothCentralManager.StopScan();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while stopping scan for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                _bluetoothCentralManager = null;

                try
                {
                    _bluetoothPeripheralManager?.StopAdvertising();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising for service " + _serviceUUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
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

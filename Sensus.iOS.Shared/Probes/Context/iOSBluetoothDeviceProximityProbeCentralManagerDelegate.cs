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
using System.Text;
using CoreBluetooth;
using Foundation;
using Sensus.Probes.Context;

namespace Sensus.iOS.Probes.Context
{
    public class iOSBluetoothDeviceProximityProbeCentralManagerDelegate : CBCentralManagerDelegate
    {
        public event EventHandler<BluetoothDeviceProximityDatum> DeviceIdEncountered;

        private iOSBluetoothDeviceProximityProbe _probe;

        public iOSBluetoothDeviceProximityProbeCentralManagerDelegate(iOSBluetoothDeviceProximityProbe probe)
        {
            _probe = probe;
        }

        public override void UpdatedState(CBCentralManager central)
        {
            // the central manager may be powered on/off by the user after the probe has been started. cover the case where this happens,
            // and start scanning if the new central manager state is powered on and the probe is running.
            if (central.State == CBCentralManagerState.PoweredOn && _probe.Running)
            {
                try
                {
                    central.ScanForPeripherals(_probe.DeviceIdService.UUID);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting scan for peripherals:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
            peripheral.DiscoveredService += (sender, e) =>
            {
                try
                {
                    if (e.Error == null)
                    {
                        // discover characteristics for newly discovered services
                        foreach (CBService service in peripheral.Services)
                        {
                            if (service.UUID.Equals(_probe.DeviceIdService.UUID))
                            {
                                peripheral.DiscoverCharacteristics(new CBUUID[] { _probe.DeviceIdCharacteristic.UUID }, service);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Error while discovering service:  " + e.Error);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while discovering characteristics:  " + ex.Message, LoggingLevel.Normal, GetType());
                    DisconnectPeripheral(central, peripheral);
                }
            };

            peripheral.DiscoveredCharacteristic += (sender, e) =>
            {
                try
                {
                    if (e.Error == null)
                    {
                        // read device ID for newly discovered characteristics
                        foreach (CBCharacteristic characteristic in e.Service.Characteristics)
                        {
                            if (characteristic.UUID.Equals(_probe.DeviceIdCharacteristic.UUID))
                            {
                                peripheral.ReadValue(characteristic);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Error while discovering characteristic:  " + e.Error);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading device ID value from peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
                    DisconnectPeripheral(central, peripheral);
                }
            };

            peripheral.UpdatedCharacterteristicValue += (sender, e) =>
            {
                try
                {
                    if (e.Error == null)
                    {
                        // characteristic should have a non-null value
                        if (e.Characteristic.Value == null)
                        {
                            throw new Exception("Null updated value for characteristic.");
                        }
                        else
                        {
                            string encounteredDeviceId = Encoding.UTF8.GetString(e.Characteristic.Value.ToArray());
                            DeviceIdEncountered?.Invoke(this, new BluetoothDeviceProximityDatum(DateTime.UtcNow, encounteredDeviceId));
                        }
                    }
                    else
                    {
                        throw new Exception("Error while updating characteristic value:  " + e.Error);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reporting encountered device ID:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    DisconnectPeripheral(central, peripheral);
                }
            };

            try
            {
                central.ConnectPeripheral(peripheral);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while connecting to peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            // discover services for newly connected peripheral
            try
            {
                peripheral.DiscoverServices(new CBUUID[] { _probe.DeviceIdService.UUID });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while discovering services." + ex.Message, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(central, peripheral);
            }
        }

        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            if (error != null)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to connect peripheral:  " + error, LoggingLevel.Normal, GetType());
            }
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Disconnected peripheral.", LoggingLevel.Normal, GetType());
        }

        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            SensusServiceHelper.Get().Logger.Log("Will restore state.", LoggingLevel.Normal, GetType());
        }

        private void DisconnectPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            try
            {
                central.CancelPeripheralConnection(peripheral);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}

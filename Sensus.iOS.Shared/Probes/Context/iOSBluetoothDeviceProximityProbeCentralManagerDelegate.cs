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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Sensus.Probes.Context;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    /// iOS BLE central (scanner/client) delegate class. Receives events related to BLE scanning and  
    /// characteristic reading.
    /// </summary>
    public class iOSBluetoothDeviceProximityProbeCentralManagerDelegate : CBCentralManagerDelegate
    {
        private CBMutableService _service;
        private CBMutableCharacteristic _characteristic;
        private iOSBluetoothDeviceProximityProbe _probe;
        private List<Tuple<CBPeripheral, CBCentralManager, DateTimeOffset>> _peripheralCentralTimestamps;

        public iOSBluetoothDeviceProximityProbeCentralManagerDelegate(CBMutableService service, CBMutableCharacteristic characteristic, iOSBluetoothDeviceProximityProbe probe)
        {
            _service = service;
            _characteristic = characteristic;
            _probe = probe;
            _peripheralCentralTimestamps = new List<Tuple<CBPeripheral, CBCentralManager, DateTimeOffset>>();
        }

        public override void UpdatedState(CBCentralManager central)
        {
            // the central manager may be powered on/off by the user after the probe has been started. cover the case where this happens,
            // and start scanning if the new central manager state is powered on and the probe is running.
            if (central.State == CBCentralManagerState.PoweredOn && _probe.Running)
            {
                try
                {
                    central.ScanForPeripherals(_service.UUID);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting scan for peripherals:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
            lock (_peripheralCentralTimestamps)
            {
                SensusServiceHelper.Get().Logger.Log("Discovered peripheral:  " + peripheral.Identifier, LoggingLevel.Normal, GetType());
                _peripheralCentralTimestamps.Add(new Tuple<CBPeripheral, CBCentralManager, DateTimeOffset>(peripheral, central, DateTimeOffset.UtcNow));
            }
        }

        public async Task<List<Tuple<string, DateTimeOffset>>> ReadPeripheralCharacteristicValuesAsync(CancellationToken cancellationToken)
        {
            List<Tuple<string, DateTimeOffset>> characteristicValueTimestamps = new List<Tuple<string, DateTimeOffset>>();

            // copy list of peripherals to read. note that the same device may be reported more than once. read each once.
            List<Tuple<CBPeripheral, CBCentralManager, DateTimeOffset>> peripheralCentralTimestamps;
            lock (_peripheralCentralTimestamps)
            {
                peripheralCentralTimestamps = _peripheralCentralTimestamps.GroupBy(peripheralCentralTimestamp => peripheralCentralTimestamp.Item1.Identifier).Select(group => group.First()).ToList();
            }

            _probe.ReadAttemptCount += peripheralCentralTimestamps.Count;

            // read characteristic from each peripheral
            foreach (Tuple<CBPeripheral, CBCentralManager, DateTimeOffset> peripheralCentralTimestamp in peripheralCentralTimestamps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                TaskCompletionSource<string> readCompletionSource = new TaskCompletionSource<string>();

                CBPeripheral peripheral = peripheralCentralTimestamp.Item1;
                CBCentralManager central = peripheralCentralTimestamp.Item2;
                DateTimeOffset timestamp = peripheralCentralTimestamp.Item3;

                #region discover services
                peripheral.DiscoveredService += (sender, e) =>
                {
                    try
                    {
                        if (e.Error == null)
                        {
                            SensusServiceHelper.Get().Logger.Log("Discovered services. Discovering characteristics...", LoggingLevel.Normal, GetType());

                            // discover characteristics for newly discovered services that match the one we're looking for
                            foreach (CBService service in peripheral.Services)
                            {
                                if (service.UUID.Equals(_service.UUID))
                                {
                                    peripheral.DiscoverCharacteristics(new CBUUID[] { _characteristic.UUID }, service);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Error while discovering services:  " + e.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while discovering characteristics:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                };
                #endregion

                #region discover characteristics
                peripheral.DiscoveredCharacteristic += (sender, e) =>
                {
                    try
                    {
                        if (e.Error == null)
                        {
                            SensusServiceHelper.Get().Logger.Log("Discovered characteristics. Reading value...", LoggingLevel.Normal, GetType());

                            // read characteristic value for newly discovered characteristics that match the one we're looking for
                            foreach (CBCharacteristic characteristic in e.Service.Characteristics)
                            {
                                if (characteristic.UUID.Equals(_characteristic.UUID))
                                {
                                    peripheral.ReadValue(characteristic);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Error while discovering characteristics:  " + e.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while reading characteristic values from peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                };
                #endregion

                #region update characteristic value
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
                                SensusServiceHelper.Get().Logger.Log("Value read.", LoggingLevel.Normal, GetType());
                                string characteristicValue = Encoding.UTF8.GetString(e.Characteristic.Value.ToArray());
                                readCompletionSource.SetResult(characteristicValue);
                            }
                        }
                        else
                        {
                            throw new Exception("Error while updating characteristic value:  " + e.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while reporting characteristic value:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                };
                #endregion

                try
                {
                    SensusServiceHelper.Get().Logger.Log("Connecting to peripheral...", LoggingLevel.Normal, GetType());
                    central.ConnectPeripheral(peripheral);

                    string characteristicValue = await BluetoothDeviceProximityProbe.CompleteReadAsync(readCompletionSource, cancellationToken);

                    if (characteristicValue != null)
                    {
                        characteristicValueTimestamps.Add(new Tuple<string, DateTimeOffset>(characteristicValue, timestamp));
                        _probe.ReadSuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading peripheral:  " + ex, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    DisconnectPeripheral(central, peripheral);
                }
            }

            return characteristicValueTimestamps;
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            SensusServiceHelper.Get().Logger.Log("Connected to peripheral. Discovering its services...", LoggingLevel.Normal, GetType());

            try
            {
                peripheral.DiscoverServices(new CBUUID[] { _service.UUID });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while discovering services:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        private void DisconnectPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Disconnecting peripheral...", LoggingLevel.Normal, GetType());
                central.CancelPeripheralConnection(peripheral);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Disconnected peripheral:  " + (error?.ToString() ?? "[no error]"), LoggingLevel.Normal, GetType());
        }

        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Failed to connect peripheral:  " + (error?.ToString() ?? "[no error]"), LoggingLevel.Normal, GetType());
        }

        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            SensusServiceHelper.Get().Logger.Log("Will restore state.", LoggingLevel.Normal, GetType());
        }
    }
}
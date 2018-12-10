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
using System.Collections.Generic;
using System.Text;
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
        public EventHandler<BluetoothCharacteristicReadArgs> CharacteristicRead;

        private CBMutableService _service;
        private CBMutableCharacteristic _characteristic;
        private iOSBluetoothDeviceProximityProbe _probe;
        private List<CBPeripheral> _peripherals;

        public iOSBluetoothDeviceProximityProbeCentralManagerDelegate(CBMutableService service, CBMutableCharacteristic characteristic, iOSBluetoothDeviceProximityProbe probe)
        {
            _service = service;
            _characteristic = characteristic;
            _probe = probe;
            _peripherals = new List<CBPeripheral>();
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
            // we've observed that, if we do not hold on to a reference to the peripheral, it gets cleaned up by GC and the subsequent connect/read
            // operations do not go through. this has also been observed by others:  https://stackoverflow.com/questions/21466245/cbcentralmanager-connecting-to-a-cbperipheralmanager-connects-then-disconnects
            // there is no need to clear this collection out after we process the peripheral, because the current object will be disposed after the
            // scan has completed. a new object of the current type will be initialized when the next scan beings, and around and around we go.
            lock (_peripherals)
            {
                _peripherals.Add(peripheral);
            }

            peripheral.DiscoveredService += (sender, e) =>
            {
                try
                {
                    if (e.Error == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Discovered service. Discovering characteristics...", LoggingLevel.Normal, GetType());

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
                        throw new Exception("Error while discovering characteristics:  " + e.Error);
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
                        SensusServiceHelper.Get().Logger.Log("Discovered characteristic. Reading value...", LoggingLevel.Normal, GetType());

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
                        throw new Exception("Error while reading characteristic values:  " + e.Error);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading characteristic values from peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
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
                            SensusServiceHelper.Get().Logger.Log("Value read.", LoggingLevel.Normal, GetType());

                            string characteristicValue = Encoding.UTF8.GetString(e.Characteristic.Value.ToArray());
                            CharacteristicRead?.Invoke(this, new BluetoothCharacteristicReadArgs(characteristicValue, DateTime.UtcNow));
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
                SensusServiceHelper.Get().Logger.Log("Discovered peripheral. Connecting to it...", LoggingLevel.Normal, GetType());

                central.ConnectPeripheral(peripheral);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while connecting to peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            SensusServiceHelper.Get().Logger.Log("Connected to peripheral. Discovering its services...", LoggingLevel.Normal, GetType());

            // discover services for newly connected peripheral
            try
            {
                peripheral.DiscoverServices(new CBUUID[] { _service.UUID });
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while discovering services:  " + ex.Message, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(central, peripheral);
            }
        }

        private void DisconnectPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Cancelling peripheral connection...", LoggingLevel.Normal, GetType());

                central.CancelPeripheralConnection(peripheral);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Failed to connect peripheral:  " + (error?.ToString() ?? "[no error]"), LoggingLevel.Normal, GetType());
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Disconnected peripheral:  " + (error?.ToString() ?? "[no error]"), LoggingLevel.Normal, GetType());
        }

        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            SensusServiceHelper.Get().Logger.Log("Will restore state.", LoggingLevel.Normal, GetType());
        }
    }
}

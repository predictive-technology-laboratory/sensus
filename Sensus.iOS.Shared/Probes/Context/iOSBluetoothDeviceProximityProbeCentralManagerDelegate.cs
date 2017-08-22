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
            base.DiscoveredPeripheral(central, peripheral, advertisementData, RSSI);

            // if we discover a sensus peripheral, read and store its device id
            try
            {
                central.ConnectPeripheral(peripheral);

                if (peripheral.IsConnected)
                {
                    CBMutableCharacteristic deviceIdRead = new CBMutableCharacteristic(_probe.DeviceIdCharacteristic.UUID,
                                                                                       _probe.DeviceIdCharacteristic.Properties,
                                                                                       null,
                                                                                       _probe.DeviceIdCharacteristic.Permissions);
                    peripheral.ReadValue(deviceIdRead);
                    string encounteredDeviceId = Encoding.UTF8.GetString(deviceIdRead.Value.ToArray());
                    DeviceIdEncountered?.Invoke(this, new BluetoothDeviceProximityDatum(DateTime.UtcNow, encounteredDeviceId));
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to read device ID characteristic from Sensus BLE peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            base.ConnectedPeripheral(central, peripheral);

            SensusServiceHelper.Get().Logger.Log("Connected peripheral.", LoggingLevel.Normal, GetType());
        }

        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            base.FailedToConnectPeripheral(central, peripheral, error);

            if (error != null)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to connect peripheral:  " + error, LoggingLevel.Normal, GetType());
            }
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            base.DisconnectedPeripheral(central, peripheral, error);

            SensusServiceHelper.Get().Logger.Log("Disconnected peripheral.", LoggingLevel.Normal, GetType());
        }

        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            base.WillRestoreState(central, dict);

            SensusServiceHelper.Get().Logger.Log("Will restore state.", LoggingLevel.Normal, GetType());
        }
    }
}

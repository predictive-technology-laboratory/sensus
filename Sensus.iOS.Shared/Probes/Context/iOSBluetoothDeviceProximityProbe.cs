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
        private iOSBluetoothDeviceProximityProbeCentralManagerDelegate _bluetoothCentralManagerDelegate;
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

        public iOSBluetoothDeviceProximityProbe()
        {
            _bluetoothCentralManagerDelegate = new iOSBluetoothDeviceProximityProbeCentralManagerDelegate(this);

            _bluetoothCentralManagerDelegate.DeviceIdEncountered += async (sender, bluetoothDeviceProximityDatum) =>
            {
                await StoreDatumAsync(bluetoothDeviceProximityDatum);
            };
        }

        protected override void Initialize()
        {
            // the following code relies on SensusServiceHelper singleton, which will not be available above in the constructor.

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
            _bluetoothCentralManager = new CBCentralManager(new iOSBluetoothDeviceProximityProbeCentralManagerDelegate(this),
                                                            DispatchQueue.MainQueue,
                                                            NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBCentralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
        }

        protected override void StopCentral()
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Stopping scan.", LoggingLevel.Normal, GetType());
                _bluetoothCentralManager.StopScan();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping scanning for service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }
        #endregion

        #region peripheral
        protected override void StartPeripheral()
        {
            _bluetoothPeripheralManager = new CBPeripheralManager(new iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(this),
                                                                  DispatchQueue.MainQueue,
                                                                  NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBPeripheralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
        }

        protected override void StopPeripheral()
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Removing service.", LoggingLevel.Normal, GetType());
                _bluetoothPeripheralManager.RemoveService(_deviceIdService);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing service " + _deviceIdService.UUID + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            try
            {
                SensusServiceHelper.Get().Logger.Log("Stopping advertising.", LoggingLevel.Normal, GetType());
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

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
using CoreBluetooth;
using CoreFoundation;
using Foundation;
using System.Text;
using Newtonsoft.Json;
using Sensus.Context;

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

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

        public iOSBluetoothDeviceProximityProbe()
        {
            _bluetoothCentralManagerDelegate = new iOSBluetoothDeviceProximityProbeCentralManagerDelegate(this);

            _bluetoothCentralManagerDelegate.DeviceIdEncountered += async (sender, bluetoothDeviceProximityDatum) =>
            {
                // we have no cancellation token. thus, all that can happen here is that the datum is stored locally
                // and surveys are triggered (if any are defined). size- and force-commits will not result, and this
                // is important because we might be currently executing from the background on a bluetooth scan result.
                await StoreDatumAsync(bluetoothDeviceProximityDatum, null);
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

        #region central -- scan
        protected override void StartScan()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    _bluetoothCentralManager = new CBCentralManager(_bluetoothCentralManagerDelegate,
                                                                    DispatchQueue.MainQueue,
                                                                    NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBCentralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting scanning:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        protected override void StopScan()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {                
                try
                {
                    _bluetoothCentralManager?.StopScan();
                }
                catch(Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping scanning:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _bluetoothCentralManager = null;
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
                    _bluetoothPeripheralManager = new CBPeripheralManager(new iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(this),
                                                                          DispatchQueue.MainQueue,
                                                                          NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBPeripheralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        protected override void StopAdvertising()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
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
            });
        }
        #endregion
    }
}
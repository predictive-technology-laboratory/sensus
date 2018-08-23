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
using System.Threading.Tasks;
using System.Threading;

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
    /// See the Android subclass of <see cref="BluetoothDeviceProximityProbe"/> for additional information.
    /// </summary>
    public class iOSBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
    {
        private CBMutableService _deviceIdService;
        private CBMutableCharacteristic _deviceIdCharacteristic;
        private CBCentralManager _bluetoothCentralManager;
        private CBPeripheralManager _bluetoothPeripheralManager;

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

        protected override void Initialize()
        {
            // the following code relies on SensusServiceHelper singleton, which will not be available above in the constructor.

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
        protected override Task ScanAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    try
                    {
                        iOSBluetoothDeviceProximityProbeCentralManagerDelegate bluetoothCentralManagerDelegate = new iOSBluetoothDeviceProximityProbeCentralManagerDelegate(_deviceIdService, _deviceIdCharacteristic, this);

                        bluetoothCentralManagerDelegate.CharacteristicRead += (sender, e) =>
                        {
                            lock (EncounteredDeviceData)
                            {
                                EncounteredDeviceData.Add(new BluetoothDeviceProximityDatum(e.Timestamp, e.Value));
                            }
                        };

                        _bluetoothCentralManager = new CBCentralManager(bluetoothCentralManagerDelegate,
                                                                        DispatchQueue.MainQueue,
                                                                        NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(false), CBCentralManager.OptionShowPowerAlertKey));  // the base class handles prompting using to turn on bluetooth and stops the probe if the user does not.
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while starting scanning:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                });

                await Task.Delay(ScanDurationMS, cancellationToken);

                StopScan();
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
                    _bluetoothPeripheralManager = new CBPeripheralManager(new iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(_deviceIdService, _deviceIdCharacteristic, this),
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
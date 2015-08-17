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
using SensusService.Probes.User;
using Empatica.iOS;
using SensusService;
using System.Collections.Generic;
using System.Linq;
using SensusService.Probes.User.Empatica;
using System.Threading;

namespace Sensus.iOS.Probes.User.Empatica
{
    public class iOSEmpaticaWristbandProbe : EmpaticaWristbandProbe
    {
        private iOSEmpaticaSystemListener _empaticaListener;
        private List<EmpaticaDeviceManager> _discoveredDevices;
        private ManualResetEvent _deviceDiscoveryWait;

        public iOSEmpaticaWristbandProbe()
        {
            _empaticaListener = new iOSEmpaticaSystemListener();
            _discoveredDevices = new List<EmpaticaDeviceManager>();
            _deviceDiscoveryWait = new ManualResetEvent(false);

            _empaticaListener.DevicesDiscovered += (o, devices) =>
            {
                SensusServiceHelper.Get().Logger.Log("Discovered " + devices.Length + " Empatica devices (" + devices.Count(d => d.Allowed) + " allowable).", LoggingLevel.Normal, GetType());

                lock (_discoveredDevices)
                    foreach (EmpaticaDeviceManager device in devices)
                        if (device.Allowed)
                            _discoveredDevices.Add(device);

                _deviceDiscoveryWait.Set();
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrWhiteSpace(EmpaticaKey))
                throw new Exception("Failed to start Empatica probe:  Empatica API key must be supplied.");

            ManualResetEvent authenticateWait = new ManualResetEvent(false);
            Exception authenticateException = null;

            lock (_discoveredDevices)
                _discoveredDevices.Clear();
            
            _deviceDiscoveryWait.Reset();
            Exception deviceDiscoveryException = null;

            try
            {
                EmpaticaAPI.AuthenticateWithAPIKey(EmpaticaKey, (success, message) =>
                    {
                        if (success)
                        {
                            #region start device discovery
                            try
                            {
                                EmpaticaAPI.DiscoverDevices(_empaticaListener);
                            }
                            catch (Exception ex)
                            {
                                deviceDiscoveryException = new Exception("Failed to start Empatica device discovery:  " + ex.Message);
                                _deviceDiscoveryWait.Set();
                            }
                            #endregion
                        }
                        else
                        {
                            authenticateException = new Exception("Empatica authenticate failure:  " + message.ToString());
                        }

                        authenticateWait.Set();
                    });
            }
            catch (Exception ex)
            {
                authenticateException = new Exception("Failed to start Empatica authentication:  " + ex.Message);
                authenticateWait.Set();
            }

            authenticateWait.WaitOne();

            if (authenticateException == null)
            {
                SensusServiceHelper.Get().Logger.Log("Empatica authentication succeeded. Waiting for device discovery.", LoggingLevel.Normal, GetType());

                _deviceDiscoveryWait.WaitOne();

                if (deviceDiscoveryException == null)
                {
                    lock (_discoveredDevices)
                        if (_discoveredDevices.Count == 0)
                        {
                            string message = "No allowable Empatica devices found.";
                            SensusServiceHelper.Get().FlashNotificationAsync(message);
                            throw new Exception(message);
                        }
                }
                else
                    throw deviceDiscoveryException;
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log(authenticateException.Message, LoggingLevel.Normal, GetType());
                throw authenticateException;
            }                
        }

        protected override void StartListening()
        {
            lock (_discoveredDevices)
                foreach (EmpaticaDeviceManager discoveredDevice in _discoveredDevices)
                    if (discoveredDevice.DeviceStatus == DeviceStatus.Disconnected)
                        Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    discoveredDevice.ConnectWithDeviceListener(new iOSEmpaticaDeviceListener(this));
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to connect Empatica device:  " + ex.Message);
                                }
                            });
        }

        protected override void StopListening()
        {
            lock (_discoveredDevices)
                foreach (EmpaticaDeviceManager discoveredDevice in _discoveredDevices)
                    if (discoveredDevice.DeviceStatus == DeviceStatus.Connected)
                        Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    discoveredDevice.Disconnect();
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect device:  " + ex.Message);
                                }
                            });
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            lock (_discoveredDevices)
                return base.TestHealth(ref error, ref warning, ref misc) || (_discoveredDevices.Count > 0 && _discoveredDevices.All(device => device.DeviceStatus == DeviceStatus.Disconnected));
        }
    }
}
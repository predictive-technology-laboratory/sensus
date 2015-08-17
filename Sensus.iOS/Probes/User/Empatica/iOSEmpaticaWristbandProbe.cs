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
        public iOSEmpaticaWristbandProbe()
        {            
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrWhiteSpace(EmpaticaKey))
                throw new Exception("Failed to start Empatica probe:  Empatica API key must be supplied.");

            ManualResetEvent authenticateWait = new ManualResetEvent(false);
            Exception authenticateException = null;

            try
            {
                EmpaticaAPI.AuthenticateWithAPIKey(EmpaticaKey, (success, message) =>
                    {
                        if (success)
                            SensusServiceHelper.Get().Logger.Log("Empatica authentication succeeded:  " + message.ToString(), LoggingLevel.Normal, GetType());
                        else
                            authenticateException = new Exception("Empatica authenticate failed:  " + message.ToString());

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
                ConnectDevices();
            else
            {
                SensusServiceHelper.Get().Logger.Log(authenticateException.Message, LoggingLevel.Normal, GetType());
                throw authenticateException;
            } 
        }

        protected override void StartListening()
        {
            ConnectDevices();
        }

        public override void ConnectDevices()
        {
            iOSEmpaticaSystemListener empaticaListener = new iOSEmpaticaSystemListener();

            empaticaListener.DevicesDiscovered += (o, devices) =>
            {
                SensusServiceHelper.Get().Logger.Log("Discovered " + devices.Length + " Empatica devices (" + devices.Count(d => d.Allowed) + " allowable).", LoggingLevel.Normal, GetType());

                foreach (EmpaticaDeviceManager device in devices)
                    if (device.Allowed && device.DeviceStatus == DeviceStatus.Disconnected)
                        device.ConnectWithDeviceListener(new iOSEmpaticaDeviceListener(this));
            };

            try
            {
                EmpaticaAPI.DiscoverDevices(empaticaListener);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to start Empatica device discovery:  " + ex.Message);
            }
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
    }
}
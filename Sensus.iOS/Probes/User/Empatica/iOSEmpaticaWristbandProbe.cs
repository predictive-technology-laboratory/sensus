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
        private List<EmpaticaDeviceManager> _connectedDevices;

        public iOSEmpaticaWristbandProbe()
        {            
            _connectedDevices = new List<EmpaticaDeviceManager>();
        }

        protected override void AuthenticateAsync(Action<Exception> callback)
        {
            EmpaticaAPI.AuthenticateWithAPIKey(EmpaticaKey, (success, message) =>
                {
                    Exception ex = null;
                    if (success)
                        SensusServiceHelper.Get().Logger.Log("Empatica authentication succeeded:  " + message.ToString(), LoggingLevel.Normal, GetType());
                    else
                        ex = new Exception("Empatica authentication failed:  " + message.ToString());

                    callback(ex);
                });
        }            

        public override void DiscoverAndConnectDevices()
        {
            iOSEmpaticaSystemListener empaticaListener = new iOSEmpaticaSystemListener();

            empaticaListener.DevicesDiscovered += (o, devices) =>
            {
                // there is a lag of a few seconds between initiation of device discovery and calling of this method. if the probe is stopped in this interval,
                // we do not want to connect the devices and begin data storage. quit now if the probe was stopped.
                if (!Running)
                    return;
                    
                SensusServiceHelper.Get().Logger.Log("Discovered " + devices.Length + " Empatica devices (" + devices.Count(d => d.Allowed) + " allowed).", LoggingLevel.Normal, GetType());

                foreach (EmpaticaDeviceManager device in devices)
                    if (device.Allowed && device.DeviceStatus == DeviceStatus.Disconnected)
                        try
                        {
                            device.ConnectWithDeviceListener(new iOSEmpaticaDeviceListener(this));

                            lock (_connectedDevices)
                                _connectedDevices.Add(device);
                            
                            SensusServiceHelper.Get().Logger.Log("Connected with Empatica device \"" + device.Name + "\".", LoggingLevel.Normal, GetType());
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to connect with Empatica device \"" + device.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
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

        protected override void DisconnectDevices()
        {
            lock (_connectedDevices)
                foreach (EmpaticaDeviceManager connectedDevice in _connectedDevices)
                    if (connectedDevice.DeviceStatus == DeviceStatus.Connected || connectedDevice.DeviceStatus == DeviceStatus.Connecting)
                        Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    connectedDevice.Disconnect();
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect Empatica device \"" + connectedDevice.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                                }
                            });
        }
    }
}
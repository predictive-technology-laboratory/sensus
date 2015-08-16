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

namespace Sensus.iOS.Probes.User.Empatica
{
    public class iOSEmpaticaWristbandProbe : EmpaticaWristbandProbe
    {
        private iOSEmpaticaListener _empaticaListener;
        private List<EmpaticaDevice> _discoveredDevices;

        public iOSEmpaticaWristbandProbe()
        {
            _empaticaListener = new iOSEmpaticaListener();

            _empaticaListener.DevicesDiscovered += (o, devices) =>
            {
                SensusServiceHelper.Get().Logger.Log("Discovered " + devices.Length + " Empatica devices (" + devices.Count(d => d.Allowed) + " allowable).", LoggingLevel.Normal, GetType());

                lock (_discoveredDevices)
                    foreach (EmpaticaDevice device in devices)
                        if (device.Allowed)
                            _discoveredDevices.Add(device);
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            global::Empatica.iOS.Empatica.AuthenticateWithAPIKey(EmpaticaKey, (success, message) =>
                {
                    if (success)
                    {
                        SensusServiceHelper.Get().Logger.Log("Empatica authentication succeeded:  " + message.ToString(), LoggingLevel.Verbose, GetType());
                        global::Empatica.iOS.Empatica.DiscoverDevices(_empaticaListener);
                    }
                    else
                    {
                        string errorMessage = "Empatica authenticate failure:  " + message.ToString();
                        SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Verbose, GetType());
                        throw new Exception(errorMessage);
                    }
                }
            );
        }

        protected override void StartListening()
        {
            foreach (EmpaticaDevice discoveredDevice in _discoveredDevices)
                if (discoveredDevice.DeviceStatus == DeviceStatus.Disconnected)
                    discoveredDevice.ConnectWithDeviceListener(new iOSEmpaticaDeviceListener(this));
        }

        protected override void StopListening()
        {
            foreach (EmpaticaDevice discoveredDevice in _discoveredDevices)
                if (discoveredDevice.DeviceStatus == DeviceStatus.Connected)
                    discoveredDevice.Disconnect();
        }
    }
}
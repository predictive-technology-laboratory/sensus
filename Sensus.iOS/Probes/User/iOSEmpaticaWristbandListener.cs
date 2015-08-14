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
using Foundation;
using Empatica.iOS;

namespace Sensus.iOS.Probes.User
{
    public class iOSEmpaticaWristbandListener : NSObject
    {
        private iOSEmpaticaListener _empatica;
        private iOSEmpaticaDeviceListener _deviceListener;

        public iOSEmpaticaWristbandListener()
        {
            _empatica = new iOSEmpaticaListener();
            _empatica.DevicesDiscovered += (o, e) =>
            {
                EmpaticaDeviceManager deviceManager = e[0] as EmpaticaDeviceManager;
                deviceManager.ConnectWithDeviceDelegate(_deviceListener);
            };
            
            _deviceListener = new iOSEmpaticaDeviceListener();
            _deviceListener.Acceleration += (o, e) =>
            {
            };
        }

        public void Start(string empaticaApiKey)
        {
            EmpaticaAPI.AuthenticateWithAPIKey(empaticaApiKey);
            EmpaticaAPI.DiscoverDevicesWithDelegate(_empatica);
        }

        public void Stop()
        {
        }

        // TODO:  PrepareForBackground and PrepareForResume -- see https://www.empatica.com/docs/gettingStarted_0.7.php
    }
}


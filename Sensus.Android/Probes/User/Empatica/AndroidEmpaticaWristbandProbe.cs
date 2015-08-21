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
using Com.Empatica.Empalink;
using Com.Empatica.Empalink.Delegates;
using SensusUI.UiProperties;
using SensusService.Probes.User.Empatica;

namespace Sensus.Android.Probes.User.Empatica
{
    public class AndroidEmpaticaWristbandProbe : EmpaticaWristbandProbe
    {
        private AndroidEmpaticaWristbandListener _listener;           

        public AndroidEmpaticaWristbandProbe()
        {
            _listener = new AndroidEmpaticaWristbandListener(this);
        }

        protected override void Initialize()
        {
            // must come before base.Initialize, since the latter calls authenticate and authenticate needs the device manager to be initialized.
            _listener.Initialize();

            base.Initialize();
        }

        protected override void Authenticate()
        {
            _listener.Authenticate(EmpaticaKey);
        }

        protected override void StartBluetooth()
        {
            _listener.StartBluetooth();
        }

        public override void DiscoverAndConnectDevices()
        {
            _listener.DiscoverAndConnectDeviceAsync();
        }

        protected override void DisconnectDevices()
        {
            _listener.DisconnectDevice();
        }
    }
}
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

using SensusService.Probes.Network;
using System;
using Newtonsoft.Json;

namespace Sensus.Android.Probes.Network
{
    public class AndroidListeningWlanProbe : ListeningWlanProbe
    {
        private EventHandler<WlanDatum> _wlanConnectionChangedCallback;

        /// <summary>
        /// TODO:  Need to verify the effect of this setting. Is a WLAN binding received when the device is asleep and the router is diconnected?
        /// </summary>
        /// <value>False.</value>
        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        public AndroidListeningWlanProbe()
        {
            _wlanConnectionChangedCallback = (sender, wlanDatum) =>
                {
                    StoreDatum(wlanDatum);
                };
        }

        protected override void StartListening()
        {
            AndroidWlanBroadcastReceiver.WIFI_CONNECTION_CHANGED += _wlanConnectionChangedCallback;
        }

        protected override void StopListening()
        {
            AndroidWlanBroadcastReceiver.WIFI_CONNECTION_CHANGED -= _wlanConnectionChangedCallback;
        }
    }
}

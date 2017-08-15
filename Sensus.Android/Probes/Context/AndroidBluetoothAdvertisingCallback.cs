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

using Android.Bluetooth.LE;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothAdvertisingCallback : AdvertiseCallback
    {
        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);

            SensusServiceHelper.Get().Logger.Log("Started advertising.", LoggingLevel.Normal, GetType());
        }

        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);

            SensusServiceHelper.Get().Logger.Log("Failed to start advertising:" + errorCode, LoggingLevel.Normal, GetType());
        }
    }
}
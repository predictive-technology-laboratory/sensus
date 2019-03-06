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
using System.Threading.Tasks;
using Sensus.Probes;
using Xamarin.Forms;

namespace Sensus.Tests.Classes
{
    public class TestSensusServiceHelper : SensusServiceHelper
    {
        public override string DeviceId
        {
            get
            {
                return "asdfasdfasdfasdf";
            }
        }

        public override string DeviceManufacturer
        {
            get { return "Testing Manufacturer"; }
        }

        public override string DeviceModel
        {
            get { return "Testing Device"; }
        }

        public override bool IsCharging
        {
            get
            {
                return new Random().NextDouble() > 0.5;
            }
        }

        public override float BatteryChargePercent
        {
            get
            {
                return (float)new Random().NextDouble();
            }
        }

        public override string OperatingSystem
        {
            get
            {
                return "Android";
            }
        }

        public override string Version
        {
            get
            {
                return "vXXXX";
            }
        }

        public override bool WiFiConnected
        {
            get
            {
                return new Random().NextDouble() > 0.5;
            }
        }

        public override string PushNotificationToken => throw new NotImplementedException();

        protected override bool IsOnMainThread
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Task<bool> DisableBluetoothAsync(bool reenable, bool lowEnergy, string rationale)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> EnableBluetoothAsync(bool lowEnergy, string rationale)
        {
            throw new NotImplementedException();
        }

        public override bool EnableProbeWhenEnablingAll(Probe probe)
        {
            throw new NotImplementedException();
        }

        public override ImageSource GetQrCodeImageSource(string contents)
        {
            throw new NotImplementedException();
        }

        public override Task PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public override Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback)
        {
            throw new NotImplementedException();
        }

        public override Task SendEmailAsync(string toAddress, string subject, string message)
        {
            throw new NotImplementedException();
        }

        public override Task ShareFileAsync(string path, string subject, string mimeType)
        {
            throw new NotImplementedException();
        }

        public override Task TextToSpeechAsync(string text)
        {
            throw new NotImplementedException();
        }

        protected override Task ProtectedFlashNotificationAsync(string message)
        {
            throw new NotImplementedException();
        }

        protected override Task RegisterWithNotificationHubAsync(Tuple<string, string> hubSas)
        {
            throw new NotImplementedException();
        }

        protected override void RequestNewPushNotificationToken()
        {
            throw new NotImplementedException();
        }

        protected override Task UnregisterFromNotificationHubAsync(Tuple<string, string> hubSas)
        {
            throw new NotImplementedException();
        }
    }
}

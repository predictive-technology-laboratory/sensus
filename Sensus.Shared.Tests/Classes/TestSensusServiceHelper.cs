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
                throw new NotImplementedException();
            }
        }

        public override bool IsCharging
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string OperatingSystem
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Version
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool WiFiConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override bool IsOnMainThread
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void BringToForeground()
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

        public override void KeepDeviceAwake()
        {
            throw new NotImplementedException();
        }

        public override void LetDeviceSleep()
        {
            throw new NotImplementedException();
        }

        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public override void RunVoicePromptAsync(string prompt, Action postDisplayCallback, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public override void SendEmailAsync(string toAddress, string subject, string message)
        {
            throw new NotImplementedException();
        }

        public override void ShareFileAsync(string path, string subject, string mimeType)
        {
            throw new NotImplementedException();
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            throw new NotImplementedException();
        }

        protected override void ProtectedFlashNotificationAsync(string message, bool flashLaterIfNotVisible, TimeSpan duration, Action callback)
        {
            throw new NotImplementedException();
        }
    }
}

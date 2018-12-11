//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

        public override Task BringToForegroundAsync()
        {
            throw new NotImplementedException();
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

        public override void KeepDeviceAwake()
        {
            throw new NotImplementedException();
        }

        public override void LetDeviceSleep()
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

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
using SensusService;
using Xamarin.Geolocation;
using Xamarin;

namespace Sensus.iOS
{
    public class iOSSensusServiceHelper : SensusServiceHelper
    {
        public override bool IsCharging
        {
            get
            {
                return false;  // TODO:  Check status
            }
        }

        public override bool WiFiConnected
        {
            get
            {
                return false;  // TODO:  Check status
            }
        }

        public override string DeviceId
        {
            get
            {
                return "device";  // TODO:  Get ID
            }
        }

        public override string OperatingSystem
        {
            get
            {
                return "ios";  // TODO:  Get version
            }
        }

        protected override Geolocator Geolocator
        {
            get
            {
                return new Geolocator();
            }
        }

        public iOSSensusServiceHelper()
        {
        }

        protected override void InitializeXamarinInsights()
        {
            Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY);
        }

        protected override void ScheduleRepeatingCallback(int callbackId, int initialDelayMS, int subsequentDelayMS)
        {
        }

        protected override void ScheduleOneTimeCallback(int callbackId, int delay)
        {
        }

        protected override void CancelCallback(int callbackId, bool repeating)
        {
        }

        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
        }

        public override void ShareFileAsync(string path, string subject)
        {
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
        }

        public override void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback)
        {
        }

        public override void FlashNotificationAsync(string message, Action callback)
        {
        }

        public override void KeepDeviceAwake()
        {
        }

        public override void LetDeviceSleep()
        {
        }

        public override void UpdateApplicationStatus(string status)
        {
        }                           
    }
}


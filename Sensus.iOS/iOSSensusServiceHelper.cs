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
using SensusService.Probes.Location;
using SensusService.Probes;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Foundation;
using Xamarin.Forms;
using System.Collections.Generic;
using AVFoundation;
using System.Threading;
using Toasts.Forms.Plugin.Abstractions;
using SensusService.Probes.Movement;

namespace Sensus.iOS
{
    public class iOSSensusServiceHelper : SensusServiceHelper
    {
        public const string SENSUS_CALLBACK_REPEAT_DELAY = "SENSUS-CALLBACK-REPEAT-DELAY";

        private Dictionary<int, UILocalNotification> _callbackIdNotification;

        public override bool IsCharging
        {
            get
            {
                // TODO:  Check on actual device...is "unknown" in simulator.
                return UIDevice.CurrentDevice.BatteryState == UIDeviceBatteryState.Charging || UIDevice.CurrentDevice.BatteryState == UIDeviceBatteryState.Full;
            }
        }

        public override bool WiFiConnected
        {
            get
            {
                return NetworkConnectivity.LocalWifiConnectionStatus() == NetworkStatus.ReachableViaWiFiNetwork;
            }
        }

        public override string DeviceId
        {
            get
            {
                return UIDevice.CurrentDevice.IdentifierForVendor.AsString();
            }
        }

        public override string OperatingSystem
        {
            get
            {
                return UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
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
            _callbackIdNotification = new Dictionary<int, UILocalNotification>();
        }

        protected override void InitializeXamarinInsights()
        {
            Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY);
        }

        public override bool Use(Probe probe)
        {
            return !(probe is PollingLocationProbe) &&  // polling isn't supported very well in iOS
                   !(probe is PollingSpeedProbe);       // ditto
        }

        #region callback scheduling
        protected override void ScheduleRepeatingCallback(int callbackId, int initialDelayMS, int repeatDelayMS)
        {
            ScheduleCallbackAsync(callbackId, initialDelayMS, true, repeatDelayMS);
        }

        protected override void ScheduleOneTimeCallback(int callbackId, int delayMS)
        {
            ScheduleCallbackAsync(callbackId, delayMS, false, -1);
        }

        public override void RescheduleRepeatingCallback(int callbackId, int initialDelayMS, int repeatDelayMS)
        {
            UnscheduleCallbackAsync(callbackId, true, () =>
                {
                    ScheduleRepeatingCallback(callbackId, initialDelayMS, repeatDelayMS);
                });
        }

        private void ScheduleCallbackAsync(int callbackId, int delayMS, bool repeating, int repeatDelayMS)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    UILocalNotification notification = new UILocalNotification
                    {
                        FireDate = DateTime.UtcNow.AddMilliseconds((double)delayMS).ToNSDate(),
                        SoundName = UILocalNotification.DefaultSoundName
                    };

                    notification.UserInfo = new NSDictionary(
                        SENSUS_CALLBACK_KEY, true, 
                        SENSUS_CALLBACK_ID_KEY, callbackId,
                        SENSUS_CALLBACK_REPEATING_KEY, repeating,
                        SENSUS_CALLBACK_REPEAT_DELAY, repeatDelayMS);

                    if(repeating)
                        lock(_callbackIdNotification)
                            _callbackIdNotification.Add(callbackId, notification);

                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                });
        }

        protected override void UnscheduleCallbackAsync(int callbackId, bool repeating, Action callback)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    lock (_callbackIdNotification)
                    {
                        // there are race conditions on this collection, and the key might be removed elsewhere
                        if (_callbackIdNotification.ContainsKey(callbackId))
                        {
                            UIApplication.SharedApplication.CancelLocalNotification(_callbackIdNotification[callbackId]);
                            _callbackIdNotification.Remove(callbackId);
                        }
                    }

                    if (callback != null)
                        callback();
                });
        }

        public void RefreshCallbackNotificationsAsync()
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    lock (_callbackIdNotification)
                        foreach (UILocalNotification notification in _callbackIdNotification.Values)
                        {
                            UIApplication.SharedApplication.CancelLocalNotification(notification);
                            UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                        }
                });
        }
        #endregion

        public override void ShareFileAsync(string path, string subject)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    // TODO:  Test on physical device. Crashes simulator.
                    UIActivityViewController shareActivity = new UIActivityViewController(new NSObject[] { new NSString("Subject"), NSUrl.FromFilename(path) }, null);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(shareActivity, true, null);
                });
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            new Thread(() =>
                {
                    // TODO:  Test on physical device.
                    new AVSpeechSynthesizer().SpeakUtterance(new AVSpeechUtterance(text));

                }).Start();
        }

        public override void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback)
        {
            // TODO
        }

        public override void FlashNotificationAsync(string message, Action callback)
        {
            DependencyService.Get<IToastNotificator>().Notify(ToastNotificationType.Info, "", message + Environment.NewLine, TimeSpan.FromSeconds(2));
        }

        #region methods not implemented in ios
        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
        }

        public override void UpdateApplicationStatus(string status)
        {
        }   

        public override void KeepDeviceAwake()
        {
        }

        public override void LetDeviceSleep()
        {
        }                        
        #endregion
    }
}


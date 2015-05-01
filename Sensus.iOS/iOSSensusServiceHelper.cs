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
        public const string SENSUS_CALLBACK_ACTIVATION_ID = "SENSUS-CALLBACK-ACTIVATION-ID";

        private Dictionary<string, UILocalNotification> _callbackIdNotification;
        private string _activationId;

        public string ActivationId
        {
            get
            {
                return _activationId;
            }
            set
            {
                _activationId = value;
            }
        }

        public override bool IsCharging
        {
            get
            {
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
            _callbackIdNotification = new Dictionary<string, UILocalNotification>();

            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
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
        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, string userNotificationMessage)
        {
            ScheduleCallbackAsync(callbackId, initialDelayMS, true, repeatDelayMS, userNotificationMessage);
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS, string userNotificationMessage)
        {
            ScheduleCallbackAsync(callbackId, delayMS, false, -1, userNotificationMessage);
        }
                   
        private void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, string userNotificationMessage)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    UILocalNotification notification = new UILocalNotification
                    {
                        FireDate = DateTime.UtcNow.AddMilliseconds((double)delayMS).ToNSDate(),                        
                        AlertBody = userNotificationMessage,
                        UserInfo = GetNotificationUserInfoDictionary(callbackId, repeating, repeatDelayMS)
                    };

                    if(userNotificationMessage != null)
                        notification.SoundName = UILocalNotification.DefaultSoundName;

                    lock (_callbackIdNotification)
                        _callbackIdNotification.Add(callbackId, notification);

                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);

                    Logger.Log("Callback " + callbackId + " scheduled for " + notification.FireDate + " (" + (repeating ? "repeating" : "one-time") + ").", LoggingLevel.Debug, GetType());
                });
        }

        protected override void UnscheduleCallback(string callbackId, bool repeating)
        {            
            lock (_callbackIdNotification)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UILocalNotification notification;
                if (_callbackIdNotification.TryGetValue(callbackId, out notification))
                {
                    Device.BeginInvokeOnMainThread(() =>
                        {
                            UIApplication.SharedApplication.CancelLocalNotification(notification);
                        });
                    
                    _callbackIdNotification.Remove(callbackId);
                }
            }
        }

        public void RefreshCallbackNotificationsAsync()
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    // since all notifications are about to be rescheduled, clear any pending notifications from the notification center
                    UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;

                    // this method will be called in one of three conditions:  (1) after sensus has been started and is running, (2)
                    // after sensus has been reactivated and was already running, and (3) after a start attempt was made but failed.
                    // in all three situations, there will be zero or more notifications present in the _callbackIdNotification lookup.
                    // in (1), the notifications will have just been created and will have activation IDs set to the activation ID of
                    // the current object. in (2), the notifications will have stale activation IDs. in (3), there will be no notifications.
                    // the required post-condition of this method is that any present notification objects have activation IDs set to
                    // the activation ID of the current object. so...let's make that happen.
                    lock (_callbackIdNotification)
                        foreach (string callbackId in _callbackIdNotification.Keys)
                        {
                            UILocalNotification notification = _callbackIdNotification[callbackId];

                            // get activation ID and check for condition (2) above
                            string activationId = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_ACTIVATION_ID)) as NSString).ToString();
                            if (activationId != _activationId)
                            {
                                // cancel stale notification and issue new notification using current activation ID
                                UIApplication.SharedApplication.CancelLocalNotification(notification);

                                bool repeating = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                                int repeatDelayMS = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY)) as NSNumber).Int32Value;
                                notification.UserInfo = GetNotificationUserInfoDictionary(callbackId, repeating, repeatDelayMS);

                                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                            }
                        }
                });
        }

        public NSDictionary GetNotificationUserInfoDictionary(string callbackId, bool repeating, int repeatDelayMS)
        {
            return new NSDictionary(
                SENSUS_CALLBACK_KEY, true, 
                SENSUS_CALLBACK_ID_KEY, callbackId,
                SENSUS_CALLBACK_REPEATING_KEY, repeating,
                SENSUS_CALLBACK_REPEAT_DELAY, repeatDelayMS,
                SENSUS_CALLBACK_ACTIVATION_ID, _activationId);
        }
        #endregion

        public override void ShareFileAsync(string path, string subject)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    ShareFileActivityItemSource activityItemSource = new ShareFileActivityItemSource(path, subject);
                    UIActivityViewController shareActivity = new UIActivityViewController(new NSObject[] { activityItemSource }, null);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(shareActivity, true, null);
                });
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            new Thread(() =>
                {
                    new AVSpeechSynthesizer().SpeakUtterance(new AVSpeechUtterance(text));

                    if(callback != null)
                        callback();

                }).Start();
        }

        public override void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback)
        {
            new Thread(() =>
                {
                    string input = null;
                    ManualResetEvent dialogDismissWait = new ManualResetEvent(false);

                    Device.BeginInvokeOnMainThread(() =>
                        {
                            ManualResetEvent dialogShowWait = new ManualResetEvent(false);

                            UIAlertView dialog = new UIAlertView("Sensus is requesting input...", prompt, null, "Cancel", "OK");
                            dialog.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
                            dialog.Dismissed += (o,e) => { dialogDismissWait.Set(); };
                            dialog.Presented += (o,e) => { dialogShowWait.Set(); };
                            dialog.Clicked += (o,e) => 
                                { 
                                    if(e.ButtonIndex == 1)
                                        input = dialog.GetTextField(0).Text;
                                };
                            
                            dialog.Show();

                            #region voice recognizer
                            if (startVoiceRecognizer)
                            {
                                new Thread(() =>
                                    {
                                        // wait for the dialog to be shown so it doesn't hide our speech recognizer activity
                                        dialogShowWait.WaitOne();

                                        // there's a slight race condition between the dialog showing and speech recognition showing. pause here to prevent the dialog from hiding the speech recognizer.
                                        Thread.Sleep(1000);

                                        // TODO:  Add speech recognition

                                    }).Start();
                            }
                            #endregion
                        });

                    dialogDismissWait.WaitOne();
                    callback(input);

                }).Start();
        }

        public override void IssueNotificationAsync(string message, string id)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    if (message != null)
                    {
                        UILocalNotification notification = new UILocalNotification
                        {
                            AlertTitle = "Sensus",
                            AlertBody = message,
                            FireDate = DateTime.UtcNow.ToNSDate()
                        };
                        
                        UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                    }
                });
        }

        public override void FlashNotificationAsync(string message, Action callback)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    DependencyService.Get<IToastNotificator>().Notify(ToastNotificationType.Info, "", message + Environment.NewLine, TimeSpan.FromSeconds(2));
                });
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


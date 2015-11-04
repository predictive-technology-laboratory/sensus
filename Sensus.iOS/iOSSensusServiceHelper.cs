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
using MessageUI;
using System.IO;

namespace Sensus.iOS
{
    public class iOSSensusServiceHelper : SensusServiceHelper
    {
        #region static members

        public const string SENSUS_CALLBACK_REPEAT_DELAY = "SENSUS-CALLBACK-REPEAT-DELAY";
        public const string SENSUS_CALLBACK_ACTIVATION_ID = "SENSUS-CALLBACK-ACTIVATION-ID";

        /// <summary>
        /// Cancels a UILocalNotification. This will succeed in one of two conditions:  (1) if the notification to be
        /// cancelled is scheduled (i.e., not delivered); and (2) if the notification to be cancelled has been delivered
        /// and if the object passed in is the actual notification and not, for example, the one that was passed to
        /// ScheduleLocalNotification -- once passed to ScheduleLocalNotification, a copy is made and the objects won't test equal
        /// for cancellation.
        /// </summary>
        /// <param name="notification">Notification to cancel.</param>
        private static void CancelLocalNotification(UILocalNotification notification)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    string notificationId = notification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ID_KEY)).ToString();

                    // a local notification can be one of two types:  (1) scheduled, in which case it hasn't yet been delivered and should reside
                    // within the shared application's list of scheduled notifications. the tricky part here is that these notification objects
                    // aren't reference-equal, so we can't just pass `notification` to CancelLocalNotification. instead, we must search for the 
                    // notification by id and cancel the appropriate scheduled notification object.
                    bool notificationCanceled = false;
                    foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                    {
                        string scheduledNotificationId = scheduledNotification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ID_KEY)).ToString();
                        if (scheduledNotificationId == notificationId)
                        {
                            UIApplication.SharedApplication.CancelLocalNotification(scheduledNotification);
                            notificationCanceled = true;
                        }
                    }

                    // if we didn't cancel the notification above, then it isn't scheduled and should have already been delivered. if it has been 
                    // delivered, then our only option for cancelling it is to pass `notification` itself to CancelLocalNotification. this assumes
                    // that `notification` is the actual notification object and not, for example, the one originally passed to ScheduleLocalNotification.
                    if (!notificationCanceled)
                        UIApplication.SharedApplication.CancelLocalNotification(notification);
                });
        }

        #endregion

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

            if (Insights.IsInitialized)
                Insights.Identify(DeviceId, "Device ID", DeviceId);
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

                    // user info can be null if we don't have an activation ID...don't schedule the notification if this happens.
                    if (notification.UserInfo == null)
                        return;

                    if (userNotificationMessage != null)
                        notification.SoundName = UILocalNotification.DefaultSoundName;

                    lock (_callbackIdNotification)
                        _callbackIdNotification.Add(callbackId, notification);

                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);

                    Logger.Log("Callback " + callbackId + " scheduled for " + notification.FireDate + " (" + (repeating ? "repeating" : "one-time") + "). " + _callbackIdNotification.Count + " total callbacks in iOS service helper.", LoggingLevel.Debug, GetType());
                });
        }

        public void ServiceCallbackNotificationAsync(UILocalNotification callbackNotification)
        {
            // cancel notification (removing it from the tray), since it has served its purpose
            CancelLocalNotification(callbackNotification);

            string callbackId = (callbackNotification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ID_KEY)) as NSString).ToString();
            bool repeating = (callbackNotification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
            int repeatDelayMS = (callbackNotification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY)) as NSNumber).Int32Value;
            string activationId = (callbackNotification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ACTIVATION_ID)) as NSString).ToString();

            // only raise callback if it's from the current activation and if it is scheduled
            if (activationId != _activationId || !CallbackIsScheduled(callbackId))
                return; 
            
            // remove from platform-specific notification collection. the purpose of the platform-specific notification collection is to hold the notifications
            // between successive activations of the app. when the app is reactivated, notifications from this collection are updated with the new activation
            // id and they are rescheduled. if, in raising the callback associated with the current notification, the app is reactivated (e.g., by a call to
            // the facebook probe login manager), then the current notification will be reissued when updated via app reactivation (which will occur, e.g., when
            // the facebook login manager returns control to the app). this can lead to duplicate notifications for the same callback, or infinite cycles of app 
            // reactivation if the notification raises a callback that causes it to be reissued (e.g., in the case of facebook login).
            lock (_callbackIdNotification)
                _callbackIdNotification.Remove(callbackId);                                                

            nint taskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                {
                    // if we're out of time running in the background, cancel the callback.
                    CancelRaisedCallback(callbackId);
                });

            RaiseCallbackAsync(callbackId, repeating, false, () =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                        {
                            // notification has been serviced, so end background task
                            UIApplication.SharedApplication.EndBackgroundTask(taskId);

                            // update and schedule notification again if it was a repeating callback and is still scheduled
                            if (repeating)
                            {
                                if (CallbackIsScheduled(callbackId))
                                {
                                    callbackNotification.FireDate = DateTime.UtcNow.AddMilliseconds((double)repeatDelayMS).ToNSDate();

                                    // add back to the platform-specific notification collection, so that the notification is updated and reissued if/when the app is reactivated
                                    lock (_callbackIdNotification)
                                        _callbackIdNotification.Add(callbackId, callbackNotification);
                                
                                    UIApplication.SharedApplication.ScheduleLocalNotification(callbackNotification);
                                }
                            }
                            else
                                UnscheduleOneTimeCallback(callbackId);
                        });
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
                    CancelLocalNotification(notification);
                    _callbackIdNotification.Remove(callbackId);
                }
            }
        }

        public void UpdateCallbackNotificationActivationIdsAsync()
        {
            Device.BeginInvokeOnMainThread(() =>
                {
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

                            if (notification.UserInfo != null)
                            {
                                // get activation ID and check for condition (2) above
                                string activationId = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_ACTIVATION_ID)) as NSString).ToString();
                                if (activationId != _activationId)
                                {
                                    // reset the UserInfo to include the current activation ID
                                    bool repeating = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                                    int repeatDelayMS = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY)) as NSNumber).Int32Value;
                                    notification.UserInfo = GetNotificationUserInfoDictionary(callbackId, repeating, repeatDelayMS);

                                    // since we set the UILocalNotification's FireDate when it was constructed, if it's currently in the past it will fire immediately when scheduled again with the new activation ID.
                                    if (notification.UserInfo != null)
                                        UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                                }
                            }
                        }
                });
        }

        public NSDictionary GetNotificationUserInfoDictionary(string callbackId, bool repeating, int repeatDelayMS)
        {
            // we've seen cases where the UserInfo dictionary cannot be serialized because one of its values is null. check all nullable types
            // and return null if found.  if this happens, the UILocalNotification will never be serviced, and things won't return to normal
            // until Sensus is activated by the user and the UILocalNotifications are refreshed.
            //
            // see:  https://insights.xamarin.com/app/Sensus-Production/issues/64
            // 
            if (callbackId == null || _activationId == null)
                return null;
            
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
                    if (MFMailComposeViewController.CanSendMail)
                    {
                        MFMailComposeViewController mailer = new MFMailComposeViewController();
                        mailer.SetSubject(subject);
                        mailer.AddAttachmentData(NSData.FromUrl(NSUrl.FromFilename(path)), "application/json", Path.GetFileName(path));
                        mailer.Finished += (sender, e) => mailer.DismissViewControllerAsync(true);
                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(mailer, true, null);
                    }
                    else
                        SensusServiceHelper.Get().FlashNotificationAsync("You do not have any mail accounts configured. Please configure one before attempting to send emails from Sensus.");
                });
        }

        public override void SendEmailAsync(string toAddress, string subject, string message)
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    if (MFMailComposeViewController.CanSendMail)
                    {
                        MFMailComposeViewController mailer = new MFMailComposeViewController();
                        mailer.SetToRecipients(new string[] { toAddress });
                        mailer.SetSubject(subject);
                        mailer.SetMessageBody(message, false);
                        mailer.Finished += (sender, e) => mailer.DismissViewControllerAsync(true);
                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(mailer, true, null);
                    }
                    else
                        SensusServiceHelper.Get().FlashNotificationAsync("You do not have any mail accounts configured. Please configure one before attempting to send emails from Sensus.");
                });            
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            new Thread(() =>
                {
                    try
                    {
                        new AVSpeechSynthesizer().SpeakUtterance(new AVSpeechUtterance(text));
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to speak utterance:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                    if (callback != null)
                        callback();

                }).Start();
        }

        public override void RunVoicePromptAsync(string prompt, Action<string> callback)
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
                            dialog.Dismissed += (o, e) =>
                            {
                                dialogDismissWait.Set();
                            };
                            dialog.Presented += (o, e) =>
                            {
                                dialogShowWait.Set();
                            };
                            dialog.Clicked += (o, e) =>
                            { 
                                if (e.ButtonIndex == 1)
                                    input = dialog.GetTextField(0).Text;
                            };

                            dialog.Show();

                            #region voice recognizer

                            new Thread(() =>
                                {
                                    // wait for the dialog to be shown so it doesn't hide our speech recognizer activity
                                    dialogShowWait.WaitOne();

                                    // there's a slight race condition between the dialog showing and speech recognition showing. pause here to prevent the dialog from hiding the speech recognizer.
                                    Thread.Sleep(1000);

                                    // TODO:  Add speech recognition

                                }).Start();

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
                    DependencyService.Get<IToastNotificator>().Notify(ToastNotificationType.Info, "", message + Environment.NewLine, TimeSpan.FromSeconds(5));

                    if (callback != null)
                        callback();
                });
        }

        public override bool EnableProbeWhenEnablingAll(Probe probe)
        {
            // polling for locations doesn't work very well in iOS, since it depends on the user. don't enable probes that need location polling by default.
            return !(probe is PollingLocationProbe) &&
            !(probe is PollingSpeedProbe) &&
            !(probe is PollingPointsOfInterestProximityProbe);
        }

        public override ImageSource GetQrCodeImageSource(string contents)
        {
            return ImageSource.FromStream(() =>
                {
                    UIImage bitmap = BarcodeWriter.Write(contents);
                    MemoryStream ms = new MemoryStream();
                    bitmap.AsPNG().AsStream().CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                });
        }

        #region methods not implemented in ios

        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            FlashNotificationAsync("This is not supported on iOS.");

            new Thread(() => callback(null)).Start();
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

        public override void BringToForeground()
        {            
        }

        #endregion
    }
}
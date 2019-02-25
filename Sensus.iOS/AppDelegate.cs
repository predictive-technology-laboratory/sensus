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
using System.IO;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Sensus.UI;
using Sensus.Probes;
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.iOS.Context;
using UIKit;
using Foundation;
using Syncfusion.SfChart.XForms.iOS.Renderers;
using Sensus.iOS.Notifications.UILocalNotifications;
using Sensus.iOS.Callbacks;
using UserNotifications;
using Sensus.iOS.Notifications.UNUserNotifications;
using Sensus.iOS.Concurrent;
using Sensus.Encryption;
using System.Threading;
using System.Threading.Tasks;
using Sensus.iOS.Notifications;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        private TaskCompletionSource<UIUserNotificationSettings> _uiUserNotificationSettingsRegistrationTask;

        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;

            SensusContext.Current = new iOSSensusContext
            {
                Platform = Sensus.Context.Platform.iOS,
                MainThreadSynchronizer = new MainConcurrent(),
                SymmetricEncryption = new SymmetricEncryption(SensusServiceHelper.ENCRYPTION_KEY),
                PowerConnectionChangeListener = new iOSPowerConnectionChangeListener()
            };

            // set up scheduler / notifier depending on the iOS version.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                SensusContext.Current.CallbackScheduler = new UNUserNotificationCallbackScheduler();
                SensusContext.Current.Notifier = new UNUserNotificationNotifier();
                UNUserNotificationCenter.Current.Delegate = new UNUserNotificationDelegate();
            }
            else
            {
                SensusContext.Current.CallbackScheduler = new UILocalNotificationCallbackScheduler();
                SensusContext.Current.Notifier = new UILocalNotificationNotifier();
            }

            // initialize service helper. must come after context initialization.
            SensusServiceHelper.Initialize(() => new iOSSensusServiceHelper());

            // register for push notifications. must come after service helper initialization. if the 
            // user subsequently denies authorization to display notifications, then all notifications will
            // simply be delivered to the app silently, per the following:  https://developer.apple.com/documentation/uikit/uiapplication/1623078-registerforremotenotifications
            UIApplication.SharedApplication.RegisterForRemoteNotifications();


            // initialize stuff prior to app load
            Forms.Init();
            FormsMaps.Init();

#pragma warning disable RECS0026 // Possible unassigned object created by 'new'
            new SfChartRenderer();
#pragma warning restore RECS0026 // Possible unassigned object created by 'new'

            ZXing.Net.Mobile.Forms.iOS.Platform.Init();

            // load the app, which starts crash reporting and analytics telemetry.
            LoadApplication(new App());

#if UI_TESTING
            Forms.ViewInitialized += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.View.StyleId))
                {
                    e.NativeView.AccessibilityIdentifier = e.View.StyleId;
                }
            };

            Calabash.Start();
#endif

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            System.Threading.Tasks.Task.Run(async () =>

            {
                if (url != null)
                {
                    if (url.PathExtension == "json")
                    {
                        Protocol protocol = null;

                        if (url.Scheme == "sensuss")
                        {
                            try
                            {
                                protocol = await Protocol.DeserializeAsync(new Uri("https://" + url.AbsoluteString.Substring(url.AbsoluteString.IndexOf('/') + 2).Trim()));
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to get Sensus study from HTTPS URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                            }
                        }
                        else
                        {
                            try
                            {
                                protocol = await Protocol.DeserializeAsync(File.ReadAllBytes(url.Path));
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to get Sensus study from file URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());

                            }
                        }

                        await Protocol.DisplayAndStartAsync(protocol);

                    }
                }
            });

            // We need to handle URLs by passing them to their own OpenUrl in order to make the Facebook SSO authentication works. 
            return ApplicationDelegate.SharedInstance.OpenUrl(application, url, sourceApplication, annotation);
        }

        public override async void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);

            try
            {
                await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    // request authorization to show notifications to the user for data/survey requests.
                    bool notificationsAuthorized = false;

                    if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                    {
                        // if notifications were previously authorized and configured, there's nothing more to do.
                        UNNotificationSettings settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();

                        if (settings.BadgeSetting == UNNotificationSetting.Enabled &&
                            settings.SoundSetting == UNNotificationSetting.Enabled &&
                            settings.AlertSetting == UNNotificationSetting.Enabled)
                        {
                            notificationsAuthorized = true;
                        }
                        else
                        {
                            // request authorization for notifications. if the user previously denied authorization, this will simply return non-granted.
                            Tuple<bool, NSError> grantedError = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Alert);

                            // if the user just granted authorization, configure the notification subsystem.
                            if (grantedError.Item1)
                            {
                                // clear any current/pending notifications
                                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
                                UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

                                notificationsAuthorized = true;
                            }
                        }
                    }
                    // use the pre-10.0 approach based on UILocalNotifications. we require ios 9 or later, so we don't have to worry about 
                    // pre-8 ios as done here:  https://docs.microsoft.com/en-us/azure/notification-hubs/xamarin-notification-hubs-ios-push-notification-apns-get-started
                    else
                    {
                        // if notifications were previously authorized and configured, there's nothing more to do.
                        UIUserNotificationSettings settings = uiApplication.CurrentUserNotificationSettings;

                        if (settings.Types == (UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert))
                        {
                            notificationsAuthorized = true;
                        }
                        else
                        {
                            // request authorization for notifications. if the user previously denied authorization, this will simply return non-granted.
                            _uiUserNotificationSettingsRegistrationTask = new TaskCompletionSource<UIUserNotificationSettings>();
                            UIUserNotificationSettings notificationSettings = UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet());
                            UIApplication.SharedApplication.RegisterUserNotificationSettings(notificationSettings);
                            settings = await _uiUserNotificationSettingsRegistrationTask.Task;

                            // if the user just granted authorization, configure the notification subsystem.
                            if (settings.Types == (UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert))
                            {
                                notificationsAuthorized = true;
                            }
                        }
                    }

                    // ensure service helper is running. it is okay to call the following line multiple times, as repeats have no effect.
                    // per apple guidelines, sensus will run without notifications being authorized above, but the user's ability to 
                    // participate will certainly be reduced, as they won't be made aware of probe requests, surveys, etc.
                    await SensusServiceHelper.Get().StartAsync();

                    // update/run all callbacks
                    await (SensusContext.Current.CallbackScheduler as iOSCallbackScheduler).UpdateCallbacksAsync();

                    // disabling notifications will greatly impair the user's studies. let the user know.
                    if (!notificationsAuthorized)
                    {
                        // warn the user and help them to enable notifications  
                        UIAlertView warning = new UIAlertView("Warning", "Notifications are disabled. Please enable notifications to participate fully in your studies. Tap the button below to do this now.", default(IUIAlertViewDelegate), "Close", "Open Notification Settings");

                        warning.Dismissed += async (sender, e) =>
                        {
                            if (e.ButtonIndex == 1)
                            {
                                NSUrl notificationSettingsURL = new NSUrl(UIApplication.OpenSettingsUrlString.ToString());

                                // deprecation:  https://developer.apple.com/library/archive/releasenotes/General/WhatsNewIniOS/Articles/iOS10.html
                                if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                                {
#pragma warning disable XI0002 // Notifies you from using newer Apple APIs when targeting an older OS version
                                    await uiApplication.OpenUrlAsync(notificationSettingsURL, new UIApplicationOpenUrlOptions());
#pragma warning restore XI0002 // Notifies you from using newer Apple APIs when targeting an older OS version
                                }
                                else
                                {
#pragma warning disable XI0003 // Notifies you when using a deprecated, obsolete or unavailable Apple API
                                    uiApplication.OpenUrl(notificationSettingsURL);
#pragma warning restore XI0003 // Notifies you when using a deprecated, obsolete or unavailable Apple API
                                }
                            }
                        };

                        warning.Show();
                    }

#if UI_TESTING
                    // load and run the UI testing protocol
                    string filePath = NSBundle.MainBundle.PathForResource("UiTestingProtocol", "json");
                    using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        await Protocol.RunUiTestingProtocolAsync(file);
                    }
#endif
                });
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while activating:  " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Pre-10.0:  Handles local notification registration results.
        /// </summary>
        /// <param name="application">Application.</param>
        /// <param name="notificationSettings">Notification settings.</param>
        public override void DidRegisterUserNotificationSettings(UIApplication application, UIUserNotificationSettings notificationSettings)
        {
            // the variable should never be null, but just in case...also, use try-set as this method may be called multiple times by iOS per error reports.
            _uiUserNotificationSettingsRegistrationTask?.TrySetResult(notificationSettings);
        }

        /// <summary>
        /// Pre-10.0:  Handles notifications received when the app is in the foreground. See <see cref="UNUserNotificationDelegate.WillPresentNotification"/> for
        /// the corresponding handler for iOS 10.0 and above.
        /// </summary>
        /// <param name="application">Application.</param>
        /// <param name="notification">Notification.</param>
        public override async void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            // UILocalNotifications were obsoleted in iOS 10.0, and we should not be receiving them via this app delegate
            // method. we won't have any idea how to service them on iOS 10.0 and above. report the problem and bail.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                SensusException.Report("Received UILocalNotification in iOS 10 or later.");
            }
            else
            {
                // we're in iOS < 10.0, which means we should have a UILocal-based notifier and scheduler to handle the notification.
                UILocalNotificationNotifier notifier = SensusContext.Current.Notifier as UILocalNotificationNotifier;
                UILocalNotificationCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as UILocalNotificationCallbackScheduler;
                if (notifier == null)
                {
                    SensusException.Report("We don't have a UILocalNotificationNotifier.");
                }
                else if (callbackScheduler == null)
                {
                    SensusException.Report("We don't have a UILocalNotificationCallbackScheduler.");
                }
                else if (notification.UserInfo == null)
                {
                    SensusException.Report("Null user info passed to ReceivedLocalNotification.");
                }
                else
                {
                    // we've tried pulling some of the code below out of the UI thread, but we do not receive/process
                    // the callback notifications when doing so.
                    await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        try
                        {
                            // check for the pending survey notification
                            string notificationId = notification.UserInfo.ValueForKey(new NSString(iOSNotifier.NOTIFICATION_ID_KEY))?.ToString();
                            if (notificationId == SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID)
                            {
                                // flash a message to the user, and don't cancel the notification.
                                await SensusServiceHelper.Get().FlashNotificationAsync("A new survey is available.");
                            }
                            else if (notificationId == SensusServiceHelper.PROTOCOL_UPDATED_NOTIFICATION_ID)
                            {
                                // flash a message to the user, and don't cancel the notification.
                                await SensusServiceHelper.Get().FlashNotificationAsync("Your study was updated.");
                            }
                            else
                            {
                                // cancel notification (removing it from the tray), since it has served its purpose (e.g., as a callback notification).
                                notifier.CancelNotification(notification);
                            }

                            // service the callback if we've got one (not all notification userinfo bundles are for callbacks)
                            if (callbackScheduler.IsCallback(notification.UserInfo))
                            {
                                await callbackScheduler.ServiceCallbackAsync(notification.UserInfo);
                            }

                            // check whether the user opened the notification to open sensus, indicated by an application state that is not active. we'll
                            // also get notifications when the app is active, since we use them for timed callback events.
                            if (application.ApplicationState != UIApplicationState.Active)
                            {
                                // if the user opened the notification, display the page associated with the notification, if any. 
                                callbackScheduler.OpenDisplayPage(notification.UserInfo);

                                // provide some generic feedback if the user responded to a silent callback notification
                                if (callbackScheduler.TryGetCallback(notification.UserInfo)?.Silent ?? false)
                                {
                                    await SensusServiceHelper.Get().FlashNotificationAsync("Study Updated.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusException.Report("Exception while processing local notification (iOS < 10):  " + ex.Message, ex);
                        }
                    });
                }
            }
        }

        public override async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            // hang on to the token.
            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;
            serviceHelper.PushNotificationTokenData = deviceToken;

            // update push notification registrations. this depends on internet connectivity to S3
            // so it might hang if connectivity is poor. ensure we don't violate execution limits.
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            nint updateTaskId = application.BeginBackgroundTask(cancellationTokenSource.Cancel);
            await serviceHelper.UpdatePushNotificationRegistrationsAsync(cancellationTokenSource.Token);
            application.EndBackgroundTask(updateTaskId);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            SensusException.Report("Failed to register for remote notifications.", error == null ? null : new Exception(error.ToString()));
        }

        public override async void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            await ProcessRemoteNotificationAsync(userInfo);
        }

        public override async void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            await ProcessRemoteNotificationAsync(userInfo);

            // once the remote notification has been processed, invoke the completion handler.
            completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
        }

        private async System.Threading.Tasks.Task ProcessRemoteNotificationAsync(NSDictionary userInfo)
        {
            // set up a cancellation token for processing within limits. the token will be cancelled
            // if we run out of time or an exception is thrown in this method
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
                // the api docs indicate that we have about 30 seconds to process push notifications:  https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/pushing_updates_to_your_app_silently
                // be on the conservative side and only run for 25 seconds.
                TimeSpan processingTimeLimit = TimeSpan.FromSeconds(25);
                cancellationTokenSource.CancelAfter(processingTimeLimit);
                cancellationTokenSource.Token.Register(() =>
                {
                    SensusServiceHelper.Get().Logger.Log("Cancelled token for remote notification processing due to iOS background time limit:  " + processingTimeLimit, LoggingLevel.Normal, GetType());
                });

                string protocolId;
                string id;
                string command;
                string sound;
                string body;
                string title;

                // extract push notification information
                try
                {
                    protocolId = (userInfo[new NSString("protocol")] as NSString).ToString();
                    id = (userInfo[new NSString("id")] as NSString).ToString();
                    command = (userInfo[new NSString("command")] as NSString).ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception while extracting push notification information:  " + ex.Message);
                }

                try
                {
                    NSDictionary aps = userInfo[new NSString("aps")] as NSDictionary;
                    sound = (aps[new NSString("sound")] as NSString).ToString();
                    NSDictionary alert = aps[new NSString("alert")] as NSDictionary;
                    body = (alert[new NSString("body")] as NSString).ToString();
                    title = (alert[new NSString("title")] as NSString).ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception while extracting notification from APS object:  " + ex.Message);
                }

                try
                {
                    if (SensusContext.Current == null)
                    {
                        throw new NullReferenceException("Null context");
                    }

                    if (SensusContext.Current.Notifier == null)
                    {
                        throw new NullReferenceException("Null notifier");
                    }

                    // wait for the push notification to be processed
                    await SensusContext.Current.Notifier.ProcessReceivedPushNotificationAsync(protocolId, id, title, body, sound, command, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception while requesting notifier to process received push notification:  " + ex.Message);
                }
            }
            catch (Exception processingException)
            {
                SensusException.Report("Exception while processing remote notification:  " + processingException.Message + ". Push notification dictionary content:  " + userInfo, processingException);

                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (Exception)
                { }
            }
            finally
            {
                try
                {
                    // we're done. ensure that the time-based cancellation above does not trigger any registered listeners.
                    cancellationTokenSource.Dispose();
                }
                catch (Exception)
                { }
            }
        }

        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication uiApplication)
        {
            // scheduler will be null in the case where notifications have not been authorized
            (SensusContext.Current.CallbackScheduler as iOSCallbackScheduler)?.CancelSilentNotifications();

            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            // save app state in background
            nint saveTaskId = uiApplication.BeginBackgroundTask(() =>
            {
                // not much we can do if we run out of time...
            });

            serviceHelper.SaveAsync().ContinueWith(finishedTask =>
            {
                uiApplication.EndBackgroundTask(saveTaskId);
            });
        }

        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication uiApplication)
        {
        }

        // This method is called when the application is about to terminate. Save data, if needed.
        public override async void WillTerminate(UIApplication uiApplication)
        {
            // this method won't be called when the user kills the app using multitasking; however,
            // it should be called if the system kills the app when it's running in the background.
            // it should also be called if the system shuts down due to loss of battery power.
            // there doesn't appear to be a way to gracefully stop the app when the user kills it
            // via multitasking...we'll have to live with that. also some online resources indicate 
            // that no background time can be requested from within this method. so, instead of 
            // beginning a background task, just wait for the calls to finish.

            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            // we're going to save the service helper and its protocols/probes in the running state
            // so that they will be restarted if/when the user restarts the app. in order to properly 
            // track running time for listening probes, we need to add a stop time manually since
            // we won't call stop until after the service helper has been saved.
            foreach (Protocol protocol in serviceHelper.RegisteredProtocols)
            {
                if (protocol.State == ProtocolState.Running)
                {
                    foreach (Probe probe in protocol.Probes)
                    {
                        if (probe.Running)
                        {
                            lock (probe.StartStopTimes)
                            {
                                probe.StartStopTimes.Add(new Tuple<bool, DateTime>(false, DateTime.Now));
                            }
                        }
                    }
                }
            }

            await serviceHelper.SaveAsync();
            await serviceHelper.StopProtocolsAsync();
        }
    }
}
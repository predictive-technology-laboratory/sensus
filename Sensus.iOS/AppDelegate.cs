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
using Sensus.iOS.Callbacks;
using UserNotifications;
using Sensus.iOS.Notifications.UNUserNotifications;
using Sensus.iOS.Concurrent;
using Sensus.Encryption;
using System.Threading;
using Sensus.iOS.Notifications;
using Sensus.Notifications;
using System.Collections.Generic;
using System.Linq;
using Sensus.Probes.Location;

namespace Sensus.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			DateTime finishLaunchStartTime = DateTime.Now;

			UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;

			SensusContext.Current = new iOSSensusContext
			{
				Platform = Sensus.Context.Platform.iOS,
				MainThreadSynchronizer = new MainConcurrent(),
				SymmetricEncryption = new SymmetricEncryption(SensusServiceHelper.ENCRYPTION_KEY),
				PowerConnectionChangeListener = new iOSPowerConnectionChangeListener()
			};

			SensusContext.Current.CallbackScheduler = new iOSTimerCallbackScheduler(); // new UNUserNotificationCallbackScheduler();
			SensusContext.Current.Notifier = new UNUserNotificationNotifier();
			UNUserNotificationCenter.Current.Delegate = new UNUserNotificationDelegate();

			// we've seen cases where previously terminated runs of the app leave behind 
			// local notifications. clear these out now. any callbacks these notifications
			// would have triggered are about to be rescheduled when the app is actived.
			(SensusContext.Current.Notifier as iOSNotifier).RemoveAllNotifications();

			// initialize stuff prior to app load
			Forms.Init();
			FormsMaps.Init();

			// initialize the syncfusion charting system
#pragma warning disable RECS0026 // Possible unassigned object created by 'new'
			new SfChartRenderer();
#pragma warning restore RECS0026 // Possible unassigned object created by 'new'

			ZXing.Net.Mobile.Forms.iOS.Platform.Init();

			// load the app, which starts crash reporting and analytics telemetry.
			LoadApplication(new App());

			// we have observed that, if the  app is in the background and a push notification arrives, 
			// then ios may attempt to launch the app and call this method but only provide a few (~5) 
			// seconds for this method to return. exceeding this time results in a fored termination by 
			// ios. as we're about to deserialize a large JSON object below when initializing the service
			// helper, we need to be careful about taking up too much time. start a background task to 
			// obtain as much background time as possible and report timeouts. needs to be after app load
			// in case we need to report a timeout exception.
			nint taskId = 0;

			taskId = application.BeginBackgroundTask(() =>
			{
				SensusServiceHelper.Get().Logger.Log("Ran out of time while finishing app launch.", LoggingLevel.Normal, GetType());

				application.EndBackgroundTask(taskId);
			});

			// initialize service helper. must come after context initialization. desirable to come
			// after app loading, in case we crash. crash reporting is initialized when the app 
			// object is created. nothing in the app creating and loading loop will depend on having
			// an initialized service helper, so we should be fine.
			SensusServiceHelper.Initialize(() => new iOSSensusServiceHelper());

			// register for push notifications. must come after service helper initialization as we use
			// the serivce helper below to submit the remote notification token to the backends. if the 
			// user subsequently denies authorization to display notifications, then all remote notifications 
			// will simply be delivered to the app silently, per the following:
			//
			// https://developer.apple.com/documentation/uikit/uiapplication/1623078-registerforremotenotifications
			//
			UIApplication.SharedApplication.RegisterForRemoteNotifications();

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

			application.EndBackgroundTask(taskId);

			// must come after app load
			base.FinishedLaunching(application, launchOptions);

			// record how long we took to launch. ios is eager to kill apps that don't start fast enough, so log information
			// to help with debugging.
			DateTime finishLaunchEndTime = DateTime.Now;
			SensusServiceHelper.Get().Logger.Log("Took " + (finishLaunchEndTime - finishLaunchStartTime) + " to finish launching.", LoggingLevel.Normal, GetType());

			return true;
		}

		public override async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		{
			// hang on to the token. we register for remote notifications after initializing the helper, so this should be fine.
			if (SensusServiceHelper.Get() is iOSSensusServiceHelper serviceHelper)
			{
				serviceHelper.PushNotificationTokenData = deviceToken;

				// update push notification registrations. this depends on internet connectivity to S3
				// so it might hang if connectivity is poor. ensure we don't violate execution limits.
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				nint taskId = 0;

				taskId = application.BeginBackgroundTask(() =>
				{
					cancellationTokenSource.Cancel();

					application.EndBackgroundTask(taskId);
				});

				await serviceHelper.UpdatePushNotificationRegistrationsAsync(cancellationTokenSource.Token);

				application.EndBackgroundTask(taskId);
			}
		}

		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
		{
			if (url?.PathExtension == "json")
			{
				System.Threading.Tasks.Task.Run(async () =>
				{
					try
					{
						Protocol protocol = null;

						if (url.Scheme == "sensuss")
						{
							protocol = await Protocol.DeserializeAsync(new Uri("https://" + url.AbsoluteString.Substring(url.AbsoluteString.IndexOf('/') + 2).Trim()), true);
						}
						else
						{
							protocol = await Protocol.DeserializeAsync(File.ReadAllBytes(url.Path), true);
						}

						await Protocol.DisplayAndStartAsync(protocol);
					}
					catch (Exception ex)
					{
						InvokeOnMainThread(() =>
						{
							string message = "Failed to get study:  " + ex.Message;
							SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());

							new UIAlertView("Error", message, default(IUIAlertViewDelegate), "Close").Show();
						});
					}
				});

				return true;
			}
			else
			{
				return false;
			}
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
						notificationsAuthorized = grantedError.Item1;
					}

					// reset the badge number before starting. it appears that badge numbers from previous installations
					// and instantiations of the app hang around.
					if (notificationsAuthorized)
					{
						UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
					}

					// ensure service helper is running. it is okay to call the following line multiple times, as repeats have no effect.
					// per apple guidelines, sensus will run without notifications being authorized above, but the user's ability to 
					// participate will certainly be reduced, as they won't be made aware of probe requests, surveys, etc.
					await SensusServiceHelper.Get().StartAsync();

					// update/run all callbacks
					await (SensusContext.Current.CallbackScheduler as iOSCallbackScheduler).UpdateCallbacksOnActivationAsync();

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
								await uiApplication.OpenUrlAsync(notificationSettingsURL, new UIApplicationOpenUrlOptions());
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

		public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
		{
			SensusException.Report("Failed to register for remote notifications.", error == null ? null : new Exception(error.ToString()));
		}

		public override async void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			UIBackgroundFetchResult remoteNotificationResult;

			// set up a cancellation token for processing within limits. the token will be cancelled
			// if we run out of time or an exception is thrown in this method.
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			nint taskId = 0;

			try
			{
				// we have limited time to process remote notifications:  https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/pushing_updates_to_your_app_silently
				// start background task to (1) obtain any possible background processing time and (2) get
				// notified when background time is about to expire. we used to hard code a fixed amount
				// of time (~30 seconds per above link), but this might change without us knowing about it.
				SensusServiceHelper.Get().Logger.Log("Starting background task for remote notification processing.", LoggingLevel.Normal, GetType());

				taskId = application.BeginBackgroundTask(() =>
				{
					SensusServiceHelper.Get().Logger.Log("Cancelling token for remote notification processing due to iOS background processing limitations.", LoggingLevel.Normal, GetType());
					
					cancellationTokenSource.Cancel();

					application.EndBackgroundTask(taskId);
				});

				NSDictionary aps = userInfo[new NSString("aps")] as NSDictionary;
				NSDictionary alert = aps[new NSString("alert")] as NSDictionary;

				PushNotification pushNotification = new PushNotification
				{
					Id = (userInfo[new NSString("id")] as NSString).ToString(),
					ProtocolId = (userInfo[new NSString("protocol")] as NSString).ToString(),
					Update = bool.Parse((userInfo[new NSString("update")] as NSString).ToString()),
					Title = (alert[new NSString("title")] as NSString).ToString(),
					Body = (alert[new NSString("body")] as NSString).ToString(),
					Sound = (aps[new NSString("sound")] as NSString).ToString()
				};

				// backend key might be blank
				string backendKeyString = (userInfo[new NSString("backend-key")] as NSString).ToString();

				if (!string.IsNullOrWhiteSpace(backendKeyString))
				{
					pushNotification.BackendKey = new Guid(backendKeyString);
				}

				await SensusContext.Current.Notifier.ProcessReceivedPushNotificationAsync(pushNotification, cancellationTokenSource.Token);

				// even if the cancellation token was cancelled, we were still successful at downloading the updates.
				// any amount of update application we were able to do in addition to the download is bonus. updates
				// will continue to be applied on subsequent push notifications and health tests.
				remoteNotificationResult = UIBackgroundFetchResult.NewData;
			}
			catch (Exception ex)
			{
				SensusException.Report("Exception while processing remote notification:  " + ex.Message + ". Push notification dictionary content:  " + userInfo, ex);

				// we might have already cancelled the token, but we might have also just hit a bug. in
				// either case, cancel the token and set the result accordingly.
				try
				{
					cancellationTokenSource.Cancel();
				}
				catch (Exception)
				{ }

				remoteNotificationResult = UIBackgroundFetchResult.Failed;
			}
			finally
			{
				// we're done. ensure that the cancellation token cannot be cancelled any 
				// longer (e.g., due to background time expiring above) by disposing it.
				if (cancellationTokenSource != null)
				{
					try
					{
						cancellationTokenSource.Dispose();
					}
					catch (Exception)
					{ }
				}

				application.EndBackgroundTask(taskId);
			}

			// invoke the completion handler to let ios know that, and how, we have finished.
			completionHandler?.Invoke(remoteNotificationResult);
		}

		// This method should be used to release shared resources and it should store the application state.
		// If your application supports background exection this method is called instead of WillTerminate
		// when the user quits.
		public async override void DidEnterBackground(UIApplication application)
		{
			nint taskId = 0;

			taskId = application.BeginBackgroundTask(() =>
			{
				// not much to do if we run out of time. just report it.
				SensusServiceHelper.Get().Logger.Log("Ran out of background time while entering background.", LoggingLevel.Normal, GetType());

				application.EndBackgroundTask(taskId);
			});

			iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

			// if the callback scheduler is timer-based and gps is not running then we need to request remote notifications
			if (SensusContext.Current.CallbackScheduler is iOSTimerCallbackScheduler scheduler)
			{
				bool gpsIsRunning = SensusServiceHelper.Get().GetRunningProtocols().SelectMany(x => x.Probes).OfType<ListeningLocationProbe>().Any(x => x.Enabled);

				await scheduler.RequestNotificationsAsync(gpsIsRunning);
			}

			// save app state
			await serviceHelper.SaveAsync();

			application.EndBackgroundTask(taskId);
		}

		// This method is called when the application is about to terminate. Save data, if needed.
		public override async void WillTerminate(UIApplication application)
		{
			// this method won't be called when the user kills the app using multitasking; however,
			// it should be called if the system kills the app when it's running in the background.
			// it should also be called if the system shuts down due to loss of battery power.
			// there doesn't appear to be a way to gracefully stop the app when the user kills it
			// via multitasking...we'll have to live with that.

			SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

			// we're not going to stop the service helper before termination, so that -- if and when
			// the app relaunches -- protocols that are currently running will be restarted. therefore,
			// we need to manually add a stop time to each running probe to ensure participation 
			// rates are calculated correctly.
			foreach (Protocol protocol in serviceHelper.RegisteredProtocols)
			{
				if (protocol.State == ProtocolState.Running)
				{
					foreach (Probe probe in protocol.Probes)
					{
						if (probe.State == ProbeState.Running)
						{
							lock (probe.StartStopTimes)
							{
								probe.StartStopTimes.Add(new Tuple<bool, DateTime>(false, DateTime.Now));
							}
						}
					}
				}
			}

			// some online resources indicate that no background time can be requested from within this 
			// method. so, instead of beginning a background task, just wait for the call to finish.
			await serviceHelper.SaveAsync();
		}
	}
}
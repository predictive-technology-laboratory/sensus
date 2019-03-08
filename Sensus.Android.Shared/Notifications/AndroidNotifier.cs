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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Sensus.Notifications;

// the unit test project contains the Resource class in its namespace rather than the Sensus.Android
// namespace. include that namespace below.
#if UNIT_TEST
using Sensus.Android.Tests;
#endif

namespace Sensus.Android.Notifications
{
    public class AndroidNotifier : Notifier
    {
        public enum SensusNotificationChannel
        {
            Silent,
            Survey,
            ForegroundService,
            Default
        }

        public const int FOREGROUND_SERVICE_NOTIFICATION_ID = 1;
        public const string FOREGROUND_SERVICE_NOTIFICATION_ACTION_PAUSE = "NOTIFICATION-ACTION-PAUSE";
        public const string FOREGROUND_SERVICE_NOTIFICATION_ACTION_RESUME = "NOTIFICATION-ACTION-RESUME";

        private NotificationManager _notificationManager;
        private Notification.Builder _foregroundServiceNotificationBuilder;
        private ForegroundServiceNotificationActionReceiver _foregroundServiceNotificationActionReceiver;

        public AndroidNotifier()
        {
            _notificationManager = Application.Context.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;

            // register the notification action receiver
            _foregroundServiceNotificationActionReceiver = new ForegroundServiceNotificationActionReceiver();
            IntentFilter foregroundServiceNotificationActionIntentFilter = new IntentFilter();
            foregroundServiceNotificationActionIntentFilter.AddAction(FOREGROUND_SERVICE_NOTIFICATION_ACTION_PAUSE);
            foregroundServiceNotificationActionIntentFilter.AddAction(FOREGROUND_SERVICE_NOTIFICATION_ACTION_RESUME);
            foregroundServiceNotificationActionIntentFilter.AddCategory(Intent.CategoryDefault);
            Application.Context.RegisterReceiver(_foregroundServiceNotificationActionReceiver, foregroundServiceNotificationActionIntentFilter);

            // create notification builder for the foreground service and set initial content
            PendingIntent mainActivityPendingIntent = PendingIntent.GetActivity(Application.Context, 0, new Intent(Application.Context, typeof(AndroidMainActivity)), 0);
            _foregroundServiceNotificationBuilder = CreateNotificationBuilder(AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                        .SetSmallIcon(Resource.Drawable.ic_launcher)
                                                        .SetContentIntent(mainActivityPendingIntent)
                                                        .SetOngoing(true);
        }

        public Notification.Builder CreateNotificationBuilder(SensusNotificationChannel channel)
        {
            global::Android.Net.Uri notificationSoundURI = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            AudioAttributes notificationAudioAttributes = new AudioAttributes.Builder()
                                                                             .SetContentType(AudioContentType.Unknown)
                                                                             .SetUsage(AudioUsageKind.NotificationEvent).Build();

            long[] vibrationPattern = { 0, 250, 50, 250 };

            bool silent = GetChannelSilent(channel);

            Notification.Builder builder;

            // see the Backwards Compatibility article for more information
#if __ANDROID_26__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationManager notificationManager = Application.Context.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;

                string channelId = channel.ToString();

                if (notificationManager.GetNotificationChannel(channelId) == null)
                {
                    NotificationChannel notificationChannel = new NotificationChannel(channelId, GetChannelName(channel), GetChannelImportance(channel))
                    {
                        Description = GetChannelDescription(channel)
                    };

                    if (silent)
                    {
                        notificationChannel.SetSound(null, null);
                        notificationChannel.EnableVibration(false);
                    }
                    else
                    {
                        notificationChannel.SetSound(notificationSoundURI, notificationAudioAttributes);
                        notificationChannel.EnableVibration(true);
                        notificationChannel.SetVibrationPattern(vibrationPattern);
                    }

                    notificationManager.CreateNotificationChannel(notificationChannel);
                }

                builder = new Notification.Builder(Application.Context, channelId);
            }
            else
#endif
            {
#pragma warning disable 618
                builder = new Notification.Builder(Application.Context);

                if (silent)
                {
                    builder.SetSound(null);
                    builder.SetVibrate(null);
                }
                else
                {
                    builder.SetSound(notificationSoundURI, notificationAudioAttributes);
                    builder.SetVibrate(vibrationPattern);
                }
#pragma warning restore 618
            }

            return builder;
        }

        private string GetChannelName(SensusNotificationChannel channel)
        {
            if (channel == SensusNotificationChannel.ForegroundService)
            {
                return "Background Services";
            }
            else if (channel == SensusNotificationChannel.Survey)
            {
                return "Surveys";
            }
            else
            {
                return "Notifications";
            }
        }

        private string GetChannelDescription(SensusNotificationChannel channel)
        {
            if (channel == SensusNotificationChannel.ForegroundService)
            {
                return "Notifications about Sensus services that are running in the background";
            }
            else if (channel == SensusNotificationChannel.Survey)
            {
                return "Notifications about Sensus surveys you can take";
            }
            else
            {
                return "General Sensus notifications";
            }
        }

        private NotificationImportance GetChannelImportance(SensusNotificationChannel channel)
        {
            if (channel == SensusNotificationChannel.ForegroundService)
            {
                return NotificationImportance.Min;
            }
            else if (channel == SensusNotificationChannel.Survey)
            {
                return NotificationImportance.Max;
            }
            else if (channel == SensusNotificationChannel.Silent)
            {
                return NotificationImportance.Min;
            }
            else
            {
                return NotificationImportance.Default;
            }
        }

        private bool GetChannelSilent(SensusNotificationChannel channel)
        {
            if (channel == SensusNotificationChannel.Default)
            {
                return false;
            }
            else if (channel == SensusNotificationChannel.ForegroundService)
            {
                return true;
            }
            else if (channel == SensusNotificationChannel.Silent)
            {
                return true;
            }
            else if (channel == SensusNotificationChannel.Survey)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public override Task IssueNotificationAsync(string title, string message, string id, bool alertUser, Protocol protocol, int? badgeNumber, DisplayPage displayPage)
        {
            if (_notificationManager == null)
            {
                return Task.CompletedTask;
            }
            else if (message == null)
            {
                CancelNotification(id);
            }
            else
            {
                Intent notificationIntent = new Intent(Application.Context, typeof(AndroidMainActivity));
                notificationIntent.PutExtra(DISPLAY_PAGE_KEY, displayPage.ToString());
                PendingIntent notificationPendingIntent = PendingIntent.GetActivity(Application.Context, 0, notificationIntent, PendingIntentFlags.OneShot);

                SensusNotificationChannel notificationChannel = SensusNotificationChannel.Default;

                if (displayPage == DisplayPage.PendingSurveys)
                {
                    notificationChannel = SensusNotificationChannel.Survey;
                }

                // reset channel to silent if we're not notifying/alerting or if we're in an exclusion window
                if (!alertUser || (protocol != null && protocol.TimeIsWithinAlertExclusionWindow(DateTime.Now.TimeOfDay)))
                {
                    notificationChannel = SensusNotificationChannel.Silent;
                }

                Notification.Builder notificationBuilder = CreateNotificationBuilder(notificationChannel)
                                                               .SetContentTitle(title)
                                                               .SetContentText(message)
                                                               .SetSmallIcon(Resource.Drawable.ic_launcher)
                                                               .SetContentIntent(notificationPendingIntent)
                                                               .SetAutoCancel(true)
                                                               .SetOngoing(false);
                if (badgeNumber != null)
                {
                    notificationBuilder.SetNumber(badgeNumber.Value);
                }

                // use big-text style for long messages
                if (message.Length > 20)
                {
                    Notification.BigTextStyle bigTextStyle = new Notification.BigTextStyle();
                    bigTextStyle.BigText(message);
                    notificationBuilder.SetStyle(bigTextStyle);
                }

                _notificationManager.Notify(id, 0, notificationBuilder.Build());
            }

            return Task.CompletedTask;
        }

        public override void CancelNotification(string id)
        {
            _notificationManager.Cancel(id, 0);
        }

        /// <summary>
        /// Updates the foreground service notification builder, so that it reflects the enrollment status and participation level of the user.
        /// </summary>
        public void UpdateForegroundServiceNotificationBuilder()
        {
            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            // the service helper will be null when this method is called from OnCreate. set some generic text until
            // the service helper has a chance to load, at which time this method will be called again and we'll update
            // the notification with more detailed information.
            if (serviceHelper == null)
            {
                _foregroundServiceNotificationBuilder.SetContentTitle("Starting...");
                _foregroundServiceNotificationBuilder.SetContentText("Tap to Open Sensus.");
            }
            // after the service helper has been initialized, we'll have more information about the studies.
            else
            {
                int numRunningStudies = serviceHelper.RegisteredProtocols.Count(protocol => protocol.State == ProtocolState.Running);

                _foregroundServiceNotificationBuilder.SetContentTitle("You are enrolled in " + numRunningStudies + " " + (numRunningStudies == 1 ? "study" : "studies") + ".");

                string contentText = "";

                // although the number of studies might be greater than 0, the protocols might not yet be started (e.g., after starting sensus).
                // also, only display the percentage if at least one protocol is configured to display it.
                List<Protocol> protocolsToAverageParticipation = serviceHelper.GetRunningProtocols()
                                                                              .Where(runningProtocol => runningProtocol.DisplayParticipationPercentageInForegroundServiceNotification).ToList();
                if (protocolsToAverageParticipation.Count > 0)
                {
                    double avgParticipation = protocolsToAverageParticipation.Average(protocol => protocol.Participation) * 100;
                    contentText += "Your overall participation level is " + Math.Round(avgParticipation, 0) + "%. ";
                }

                contentText += "Tap to open Sensus.";

                _foregroundServiceNotificationBuilder.SetContentText(contentText);

                // allow user to pause/resume data collection via the notification
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    // clear current actions
                    _foregroundServiceNotificationBuilder.SetActions();

                    // add pause action
                    int numPausableProtocols = serviceHelper.RegisteredProtocols.Count(protocol => protocol.State == ProtocolState.Running && protocol.AllowPause);
                    if (numPausableProtocols > 0)
                    {
                        Intent pauseActionIntent = new Intent(FOREGROUND_SERVICE_NOTIFICATION_ACTION_PAUSE);
                        PendingIntent pauseActionPendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, pauseActionIntent, PendingIntentFlags.CancelCurrent);
                        string pauseActionTitle = "Pause " + numPausableProtocols + " " + (numPausableProtocols == 1 ? "study" : "studies") + ".";
                        _foregroundServiceNotificationBuilder.AddAction(new Notification.Action(Resource.Drawable.ic_media_pause_light, pauseActionTitle, pauseActionPendingIntent));  // note that notification actions no longer display the icon
                    }

                    // add resume action
                    int numPausedStudies = serviceHelper.RegisteredProtocols.Count(protocol => protocol.State == ProtocolState.Paused);
                    if (numPausedStudies > 0)
                    {
                        Intent resumeActionIntent = new Intent(FOREGROUND_SERVICE_NOTIFICATION_ACTION_RESUME);
                        PendingIntent resumeActionPendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, resumeActionIntent, PendingIntentFlags.CancelCurrent);
                        string resumeActionTitle = "Resume " + numPausedStudies + " " + (numPausedStudies == 1 ? "study" : "studies") + ".";
                        _foregroundServiceNotificationBuilder.AddAction(new Notification.Action(Resource.Drawable.ic_media_play_light, resumeActionTitle, resumeActionPendingIntent));  // note that notification actions no longer display the icon
                    }
                }
            }
        }

        public Notification BuildForegroundServiceNotification()
        {
            return _foregroundServiceNotificationBuilder.Build();
        }

        public void ReissueForegroundServiceNotification()
        {
            UpdateForegroundServiceNotificationBuilder();

            (Application.Context.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager).Notify(FOREGROUND_SERVICE_NOTIFICATION_ID, BuildForegroundServiceNotification());
        }
    }
}
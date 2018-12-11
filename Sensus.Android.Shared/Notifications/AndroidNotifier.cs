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

        private AndroidSensusService _service;
        private NotificationManager _notificationManager;

        public AndroidNotifier(AndroidSensusService service)
        {
            _service = service;
            _notificationManager = _service.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;
        }

        public Notification.Builder CreateNotificationBuilder(global::Android.Content.Context context, SensusNotificationChannel channel)
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
                NotificationManager notificationManager = context.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;

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

                builder = new Notification.Builder(context, channelId);
            }
            else
#endif
            {
#pragma warning disable 618
                builder = new Notification.Builder(context);

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

        /// <summary>
        /// Issues the notification.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <param name="id">Identifier of notification.</param>
        /// <param name="protocol">Protocol to check for alert exclusion time windows.</param>
        /// <param name="alertUser">If set to <c>true</c> alert user.</param>
        /// <param name="displayPage">Display page.</param>
        public override Task IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage)
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
                Intent notificationIntent = new Intent(_service, typeof(AndroidMainActivity));
                notificationIntent.PutExtra(DISPLAY_PAGE_KEY, displayPage.ToString());
                PendingIntent notificationPendingIntent = PendingIntent.GetActivity(_service, 0, notificationIntent, PendingIntentFlags.OneShot);

                SensusNotificationChannel notificationChannel = SensusNotificationChannel.Default;

                if (displayPage == DisplayPage.PendingSurveys)
                {
                    notificationChannel = SensusNotificationChannel.Survey;
                }

                // reset channel to silent if we're not alerting or if we're in an exclusion window
                if (!alertUser || (protocol != null && protocol.TimeIsWithinAlertExclusionWindow(DateTime.Now.TimeOfDay)))
                {
                    notificationChannel = SensusNotificationChannel.Silent;
                }

                Notification.Builder notificationBuilder = CreateNotificationBuilder(_service, notificationChannel)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(Resource.Drawable.ic_launcher)
                    .SetContentIntent(notificationPendingIntent)
                    .SetAutoCancel(true)
                    .SetOngoing(false);

                _notificationManager.Notify(id, 0, notificationBuilder.Build());
            }

            return Task.CompletedTask;
        }

        public override void CancelNotification(string id)
        {
            _notificationManager.Cancel(id, 0);
        }
    }
}

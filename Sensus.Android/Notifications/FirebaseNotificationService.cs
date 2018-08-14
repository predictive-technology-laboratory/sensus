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
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Sensus.Context;
using System.Linq;
using Sensus.Exceptions;
using Sensus.Notifications;
using Sensus.Callbacks;
using Sensus.Android.Callbacks;

namespace Sensus.Android.Notifications
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseNotificationService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            Task.Run(async () =>
            {
                SensusServiceHelper.Get().Logger.Log("Received push notification:  " + message, LoggingLevel.Normal, GetType());

                Protocol protocol = null;

                try
                {
                    string protocolId = message.Data["protocol"];
                    protocol = SensusServiceHelper.Get().RegisteredProtocols.Single(p => p.Id == protocolId);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to get protocol for push notification:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                // ignore the push notification if it targets a protocol that is not running. we explicitly 
                // attempt to prevent such notifications from coming through by unregistering from hubs
                // that lack running protocols and clearing the token from the backend; however, there may 
                // be race conditions that allow a push notification to be delivered to us nonetheless.
                if (!protocol.Running)
                {
                    SensusServiceHelper.Get().Logger.Log("Protocol targeted by push notification is not running.", LoggingLevel.Normal, GetType());
                    return;
                }

                // every PN should have an ID
                string id = null;
                try
                {
                    id = message.Data["id"];
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while getting push notification id:  " + ex.Message, ex);
                    return;
                }

                // if there is user-targeted information, display the notification.
                try
                {
                    string title = message.Data["title"];
                    string body = message.Data["body"];
                    string sound = message.Data["sound"];  // sound is optional

                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body))
                    {
                        SensusContext.Current.Notifier.IssueNotificationAsync(title, body, id, protocol, !string.IsNullOrWhiteSpace(sound), DisplayPage.None);
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while notifying from push notification:  " + ex.Message, ex);
                }

                // process push notification commands
                try
                {
                    string[] idParts = id.Split(':');

                    if (idParts.Length != 3)
                    {
                        throw new Exception("Invalid push notification ID format:  " + id);
                    }

                    if (idParts.First() == CallbackScheduler.SENSUS_CALLBACK_KEY)
                    {
                        string callbackId = idParts.Last();
                        string invocationId = message.Data["command"];

                        await (SensusContext.Current.CallbackScheduler as AndroidCallbackScheduler).ServiceCallbackFromPushNotificationAsync(callbackId, invocationId);
                    }
                    else
                    {
                        throw new Exception("Unrecognized push notification ID prefix:  " + idParts.First());
                    }
                }
                catch (Exception pushNotificationCommandException)
                {
                    SensusException.Report("Exception while running push notification command:  " + pushNotificationCommandException.Message, pushNotificationCommandException);
                }
            });
        }
    }
}
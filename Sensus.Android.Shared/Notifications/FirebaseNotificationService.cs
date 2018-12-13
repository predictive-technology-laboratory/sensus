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
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Sensus.Context;
using Sensus.Exceptions;
using System.Threading;

namespace Sensus.Android.Notifications
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseNotificationService : FirebaseMessagingService
    {
        public async override void OnMessageReceived(RemoteMessage message)
        {
            try
            {
                // extract push notification information
                string protocolId = message.Data["protocol"];
                string id = message.Data["id"];
                string title = message.Data["title"];
                string body = message.Data["body"];
                string sound = message.Data["sound"];
                string command = message.Data["command"];

                // wait for the push notification to be processed
                await SensusContext.Current.Notifier.ProcessReceivedPushNotificationAsync(protocolId, id, title, body, sound, command, CancellationToken.None);
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while processing remote notification:  " + ex.Message, ex);
            }
        }
    }
}
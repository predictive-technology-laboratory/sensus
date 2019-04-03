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
using Sensus.Notifications;

namespace Sensus.Android.Notifications
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseNotificationService : FirebaseMessagingService
    {
        public async override void OnMessageReceived(RemoteMessage message)
        {
            AndroidSensusServiceHelper serviceHelper = null;

            try
            {
                // based on log messages, it looks like the os might destroy the service component of the application
                // but leave the rest of the application (e.g., the service helper) intact and resident in memory. 
                // if this happens then the serivce helper will be present, but the service itself will be destroyed. 
                // this may also mean that the protocols are stopped. regardless, we desire for the service to always
                // be running, as this ensures that the app will continue as a foreground service. so, ask the os to 
                // start the service any time a push notification is received. this should be a no-op if the service
                // is already running. don't ask for the service to be stopped in case no protocols are running, as
                // it could just be the case that a push notification arrives late after the user has stopped protocols.
                AndroidSensusService.Start(false);

                serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

                // if we just started the service above, then it's likely that the service helper will not yet be 
                // initialized (it must be deserialized, which is slow). in this case, just bail out and wait for
                // the next push notification to arrive, at which time the service helper will hopefully be ready.
                if (serviceHelper == null)
                {
                    SensusServiceHelper.Get().Logger.Log("Service helper not initialized following receipt of push notification and service start.", LoggingLevel.Normal, GetType());
                    return;
                }

                // acquire wake lock before this method returns to ensure that the device does not sleep prematurely, 
                // interrupting any execution requested by the push notification. the service 
                serviceHelper.KeepDeviceAwake();

                PushNotification pushNotification = new PushNotification
                {
                    Id = message.Data["id"],
                    ProtocolId = message.Data["protocol"],
                    Update = bool.Parse(message.Data["update"]),
                    Title = message.Data["title"],
                    Body = message.Data["body"],
                    Sound = message.Data["sound"]
                };

                // backend key might be blank
                string backendKeyString = message.Data["backend-key"];
                if (!string.IsNullOrWhiteSpace(backendKeyString))
                {
                    pushNotification.BackendKey = new Guid(backendKeyString);
                }

                await SensusContext.Current.Notifier.ProcessReceivedPushNotificationAsync(pushNotification, CancellationToken.None);
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while processing remote notification:  " + ex.Message, ex);
            }
            finally
            {
                serviceHelper?.LetDeviceSleep();
            }
        }
    }
}
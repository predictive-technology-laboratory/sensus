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

using Android.App;
using Firebase.Iid;
using System.Threading.Tasks;
using System;
using Sensus.Exceptions;
using System.Threading;

namespace Sensus.Android
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseRegistrationService : FirebaseInstanceIdService
    {
        public override void OnTokenRefresh()
        {
            // as this is a service, the service helper might not be immediately available.
            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();
            if (serviceHelper != null)
            {
                // reregister for push notifications using the new token
                serviceHelper.RegisterForPushNotifications();

                // each protocol may have its own remote data store being monitored for push notification
                // requests. tokens are per device, so send the new token to each protocol's remote
                // data store.
                Task.Run(async () =>
                {
                    foreach (Protocol protocol in serviceHelper.RegisteredProtocols)
                    {
                        try
                        {
                            if (protocol.RemoteDataStore != null)
                            {
                                await protocol.RemoteDataStore.SendPushNotificationTokenAsync(FirebaseInstanceId.Instance.Token, default(CancellationToken));
                            }
                        }
                        catch (Exception sendTokenException)
                        {
                            SensusException.Report("Failed to send push notification token:  " + sendTokenException.Message, sendTokenException);
                        }
                    }
                });
            }
        }
    }
}
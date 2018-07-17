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

namespace Sensus.Android
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseRegistrationService : FirebaseInstanceIdService
    {
        public static string TOKEN { get; private set; }

        public override void OnTokenRefresh()
        {
            // hang on to the token for later
            TOKEN = FirebaseInstanceId.Instance.Token;

            // update the token on the service helper. as this is a service, the service helper 
            // might not be immediately available.
            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();
            if (serviceHelper != null)
            {
                // set token, save to disk, and register for push notifications
                serviceHelper.PushNotificationToken = TOKEN;
                serviceHelper.Save();
                serviceHelper.RegisterForPushNotifications();
            }
        }
    }
}
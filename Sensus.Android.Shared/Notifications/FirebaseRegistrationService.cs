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
using System.Threading;

namespace Sensus.Android.Notifications
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseRegistrationService : FirebaseInstanceIdService
    {
        public override async void OnTokenRefresh()
        {
            // update push notification registrations using the new token. as this 
            // is a service, we're not exactly sure when it will be started. so 
            // the service helper might not be immediately available.
            await SensusServiceHelper.Get()?.UpdatePushNotificationRegistrationsAsync(CancellationToken.None);
        }
    }
}
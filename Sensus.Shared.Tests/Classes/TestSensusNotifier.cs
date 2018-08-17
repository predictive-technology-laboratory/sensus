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
using System.Threading;
using System.Threading.Tasks;
using Sensus.Callbacks;
using Sensus.Notifications;

namespace Sensus.Tests.Classes
{
    public class TestSensusNotifier : INotifier
    {
        public void CancelNotification(string id)
        {
        }

        public Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage type)
        {
        }

        public void OpenDisplayPage(DisplayPage displayPage)
        {
        }

        public Task ProcessReceivedPushNotificationAsync(string protocolId, string id, string title, string body, string sound, string command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task TestHealthAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

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
using Sensus.Callbacks;
using System.Threading;

namespace Sensus.Tests.Classes
{
    public class TestSensusCallbackScheduler : ICallbackScheduler
    {
        public bool ContainsCallback(ScheduledCallback callback)
        {
            return false;
        }

        public ScheduledCallbackState ScheduleCallback(ScheduledCallback callback)
        {
            return ScheduledCallbackState.Unknown;           
        }

        public Task ServiceCallbackFromPushNotificationAsync(string callbackId, string invocationId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void TestHealth()
        {
        }

        public void UnscheduleCallback(ScheduledCallback callback)
        {
        }
    }
}

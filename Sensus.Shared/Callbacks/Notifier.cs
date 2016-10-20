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

namespace Sensus.Shared.Callbacks
{
    public abstract class Notifier : INotifier
    {
        // TODO:  This is used more like a notification type (Script, Probe, PendingSurvey, etc.). Changed to enumeration?
        public const string NOTIFICATION_ID_KEY = "ID";

        public abstract void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate);
    }
}
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

namespace Sensus.Notifications
{
    /// <summary>
    /// Types of <see cref="PushNotificationUpdate"/> that can be sent via push notification to Sensus.
    /// </summary>
    public enum PushNotificationUpdateType
    {
        /// <summary>
        /// The <see cref="PushNotificationUpdate.Content"/> value contains a new policy for the <see cref="Probes.User.Scripts.IScriptProbeAgent"/>.
        /// </summary>
        SurveyAgentPolicy,

        /// <summary>
        /// The <see cref="PushNotificationUpdate.Content"/> value contains new settings for the <see cref="Protocol"/>. These new settings have the following fields:
        /// 
        /// <ul>
        ///   <ul>
        ///   <li>
        ///   `property-type`:  The fully qualified name of the type defining the property to be updated. For example, 
        ///   [`Sensus.Probes.PollingProbe`](xref:Sensus.Probes.PollingProbe) contains properties that govern the behavior of all polling-style probes.
        ///   </li>
        ///   <li>
        ///   `property-name`:  The name of the property within the type indicated by `property-type` that should be set. For example, 
        ///   [`Sensus.Probes.PollingProbe.PollingSleepDurationMS`](xref:Sensus.Probes.PollingProbe.PollingSleepDurationMS) determines how long the 
        ///   <see cref="Probes.PollingProbe"/> will sleep between taking successive readings.
        ///   </li>
        ///   <li>
        ///   `target-type`:  The fully qualified name of the type whose instances should be updated. For example, a value of 
        ///   [`Sensus.Probes.Location.PollingLocationProbe`](xref:Sensus.Probes.Location.PollingLocationProbe) would update the property indicated 
        ///   with the previous fields for just the <see cref="Probes.Location.PollingLocationProbe"/>, whereas a value of 
        ///   [`Sensus.Probes.PollingProbe`](xref:Sensus.Probes.PollingProbe) would update the property for all probes that inherit from 
        ///   <see cref="Probes.PollingProbe"/>.
        ///   </li>
        ///   <li>
        ///   `value`:  Quoted string of the new value to be set for the above property and target objects.
        ///   </li>
        ///   </ul>
        /// </ul>
        /// 
        /// </summary>
        Protocol,

        /// <summary>
        /// The <see cref="PushNotificationUpdate.Content"/> value contains information about a <see cref="Callbacks.ScheduledCallback"/> that needs to run.
        /// </summary>
        Callback,

        /// <summary>
        /// The <see cref="PushNotificationUpdate.Content"/> value will be empty. This type of <see cref="PushNotificationUpdate"/> indicates that
        /// the app should attempt to clear its backlog of pending push notification requests. This is important due to background termination and 
        /// resumption dynamics on iOS. iOS will typically terminate the app overnight and during other periods of prolonged phone inactivity. At
        /// some point, iOS will then let a push notification through, which will relaunch the app into the background. Because the relaunch 
        /// occurs in the background, limited time is provided to the app. In response, we avoid performing certain long-running operations like
        /// the submission of push notification requests. We hold on to these requests and let the app relaunch as quickly as possible. However, 
        /// because the push notification requests are held back, subsequent push notifications will be discontinued thus cutting off most periodic
        /// operations of the app. The push notification backend is therefore configured to periodically send an update with this type. Upon receipt,
        /// the app will attempt to clear the backlog of push notifications that have been held back and thereby resume normal operation.
        /// </summary>
        ClearPushNotificationRequestBacklog
    }
}
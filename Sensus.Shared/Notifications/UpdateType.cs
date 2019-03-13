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
    public enum UpdateType
    {
        /// <summary>
        /// The <see cref="Update.Content"/> value contains a new policy for the <see cref="Probes.User.Scripts.IScriptProbeAgent"/>.
        /// </summary>
        SurveyAgentPolicy,

        /// <summary>
        /// The <see cref="Update.Content"/> contains new settings for the <see cref="Protocol"/>. These new settings have the following fields:
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
        /// </summary>
        Protocol
    }
}

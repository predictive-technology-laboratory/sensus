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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Status of scripts within Sensus
    /// </summary>
    public enum ScriptState
    {
        /// <summary>
        /// The script was delivered to the user and made available for taking. On Android this will be exactly
        /// the scheduled/triggered time. On iOS, this will be exactly the triggered time, but when it comes to
        /// scheduled scripts the timing depends on whether push notifications are enabled. Without push notifications
        /// the delivery time will be when the user brings the app to the foreground. With push notifications
        /// enabled, the delivery time will correspond to the arrival of the push notification, which causes the
        /// script to run.
        /// </summary>
        Delivered,

        /// <summary>
        /// The script was opened by the user after it was <see cref="Delivered"/>.
        /// </summary>
        Opened,

        /// <summary>
        /// The script was <see cref="Opened"/> by the user, but the user subsequently cancelled its completion.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The script was submitted by the user after it was <see cref="Opened"/>, though the user might not necessarily have completed all fields.
        /// </summary>
        Submitted,

        /// <summary>
        /// The script was deleted by the user after it was <see cref="Delivered"/>.
        /// </summary>
        Deleted,

        /// <summary>
        /// The script expired after it was <see cref="Delivered"/>.
        /// </summary>
        Expired
    }
}

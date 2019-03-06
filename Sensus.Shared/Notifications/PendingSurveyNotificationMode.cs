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
    public enum PendingSurveyNotificationMode
    {
        /// <summary>
        /// Do not notify the user in any way about pending surveys.
        /// </summary>
        None,

        /// <summary>
        /// Place a badge on the Sensus home screen icon indicating how many pending surveys are available.
        /// </summary>
        Badge,

        /// <summary>
        /// Use <see cref="Badge"/> plus a textual notification indicating how many pending surveys are available.
        /// </summary>
        BadgeText,

        /// <summary>
        /// Use <see cref="BadgeText"/> indicating how many pending surveys are available, plus an audio/vibration alert.
        /// </summary>
        BadgeTextAlert
    }
}

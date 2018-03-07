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

namespace Sensus
{
    /// <summary>
    /// Events tracked by the app center
    /// </summary>
    public enum TrackedEvent
    {
        /// <summary>
        /// Health information (e.g., the extent to which a probe's performance is nominal)
        /// </summary>
        Health,

        /// <summary>
        /// Warning of a noncritical state
        /// </summary>
        Warning,

        /// <summary>
        /// Critical error
        /// </summary>
        Error,

        /// <summary>
        /// Misellaneous information
        /// </summary>
        Miscellaneous
    }
}

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
    /// Modes for handling multiple deliveries of the same survey.
    /// </summary>
    public enum RunMode
    {
        /// <summary>
        /// Keep the survey delivery that is oldest.
        /// </summary>
        SingleKeepOldest,

        /// <summary>
        /// Keep the survey delivery that is newest.
        /// </summary>
        SingleKeepNewest,

        /// <summary>
        /// Keep all deliveries of the survey.
        /// </summary>
        Multiple
    };
}


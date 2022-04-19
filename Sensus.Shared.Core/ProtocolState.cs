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
    /// Protocol state.
    /// </summary>
    public enum ProtocolState
    {
        /// <summary>
        /// The <see cref="IProtocol"/> is stopped and is a candidate for <see cref="Starting"/>.
        /// </summary>
        Stopped,

        /// <summary>
        /// The <see cref="IProtocol"/> is starting up and will soon become <see cref="Running"/>.
        /// </summary>
        Starting,

        /// <summary>
        /// The <see cref="IProtocol"/> has started and is a candidate for <see cref="Stopping"/>.
        /// </summary>
        Running,

        /// <summary>
        /// The <see cref="IProtocol"/> is in the middle of stopping and will soon become <see cref="Stopped"/>.
        /// </summary>
        Stopping,

        /// <summary>
        /// The protocol has been paused, which means that it is not storing data and is not registered as <see cref="Running"/>, but is
        /// active in other ways (e.g., surveys will fire, push notifications will be received, data will be transmitted to the
        /// remote data store.
        /// </summary>
        Paused
    }
}

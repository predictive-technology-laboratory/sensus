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

using Sensus.Shared.Callbacks;
using Sensus.Shared.Concurrent;
using Sensus.Shared.Encryption;

namespace Sensus.Shared.Context
{
    /// <summary>
    /// Defines platform-specific fields that should not be serialized as part of the service helper
    /// </summary>
    public interface ISensusContext
    {
        Platform Platform { get; set; }
        IConcurrent MainThreadSynchronizer { get; set; }
        IEncryption Encryption { get; set; }
        INotifier Notifier { get; set; }
        ICallbackScheduler CallbackScheduler { get; set; }

        /// <summary>
        /// Gets or sets the activation identifier, which changes every time the user-facing app is brought to the foreground.
        /// </summary>
        /// <value>The activation identifier.</value>
        string ActivationId { get; set; }
    }
}

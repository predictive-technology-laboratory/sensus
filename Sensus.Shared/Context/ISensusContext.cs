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

using Sensus.Callbacks;
using Sensus.Concurrent;
using Sensus.Encryption;

namespace Sensus.Context
{
    /// <summary>
    /// Defines platform-specific fields that should not be serialized as part of the service helper.
    /// </summary>
    public interface ISensusContext
    {
        Platform Platform { get; set; }
        IConcurrent MainThreadSynchronizer { get; set; }
        IEncryption SymmetricEncryption { get; set; }
        INotifier Notifier { get; set; }
        ICallbackScheduler CallbackScheduler { get; set; }
        string IamAccessKey { get; set; }
        string IamAccessKeySecret { get; set; }
    }
}
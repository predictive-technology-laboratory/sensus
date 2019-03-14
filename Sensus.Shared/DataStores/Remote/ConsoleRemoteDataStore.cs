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

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.IO;
using Sensus.Extensions;
using Sensus.Notifications;
using Newtonsoft.Json.Linq;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// When using the <see cref="ConsoleRemoteDataStore"/>, all data accumulated in <see cref="Local.LocalDataStore"/> are simply ignored. This 
    /// is useful for debugging purposes and is not recommended for practical Sensus deployments since it provides no means of moving the data 
    /// off of the device.
    /// </summary>
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        [JsonIgnore]
        public override string DisplayName
        {
            get { return "Console"; }
        }

        [JsonIgnore]
        public override bool CanRetrieveWrittenData
        {
            get
            {
                return false;
            }
        }

        public override string StorageDescription
        {
            get
            {
                return base.StorageDescription ?? "Data will be discarded " + TimeSpan.FromMilliseconds(WriteDelayMS).GetFullDescription(TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS)).ToLower() + ".";
            }
        }

        public override Task WriteDataStreamAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task SendPushNotificationTokenAsync(string token, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task DeletePushNotificationTokenAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task DeletePushNotificationRequestAsync(Guid backendKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override string GetDatumKey(Datum datum)
        {
            throw new NotImplementedException();
        }

        public override Task<T> GetDatumAsync<T>(string datumKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<List<PushNotificationUpdate>> GetUpdatesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetScriptAgentPolicyAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
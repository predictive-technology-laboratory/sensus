//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.IO;
using Sensus.Extensions;
using Sensus.Notifications;

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
                return base.StorageDescription ?? "Data will be discarded " + TimeSpan.FromMilliseconds(WriteDelayMS).GetIntervalString().ToLower();
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

        public override Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task DeletePushNotificationRequestAsync(string id, CancellationToken cancellationToken)
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

        public override Task<string> GetScriptAgentPolicyAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

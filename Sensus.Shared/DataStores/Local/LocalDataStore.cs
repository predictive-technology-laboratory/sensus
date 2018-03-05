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

using Sensus.UI.UiProperties;
using System;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// A Local Data Store accumulates data on the device. Periodically, data are written from the Local Data Store to a 
    /// <see cref="Remote.RemoteDataStore"/> for permanent storage.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private bool _sizeTriggeredRemoteWriteRunning;

        private readonly object _sizeTriggeredRemoteWriteLocker = new object();

        [JsonIgnore]
        public abstract string SizeDescription { get; }

        protected LocalDataStore()
        {
            _sizeTriggeredRemoteWriteRunning = false;
        }

        public abstract Task<bool> WriteAsync(Datum datum, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether the current local data store has grown too large, and (if it has) writes the data to the remote data store.
        /// This relieves pressure on local in cases where the remote data store is not triggered frequently enough by its own callback 
        /// delays. It makes sense to call this method after writing data to the current local data
        /// store. This method will have no effect if the local data store is configured to not upload data to the remote data store.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected Task WriteToRemoteIfTooLargeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                bool commit = false;

                lock (_sizeTriggeredRemoteWriteLocker)
                {
                    if (TooLarge() && !_sizeTriggeredRemoteWriteRunning)
                    {
                        _sizeTriggeredRemoteWriteRunning = true;
                        commit = true;
                    }
                }

                try
                {
                    if (commit)
                    {
                        SensusServiceHelper.Get().Logger.Log("Running size-triggered commit to remote.", LoggingLevel.Normal, GetType());
                        await Protocol.RemoteDataStore.WriteAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to run size-triggered commit to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _sizeTriggeredRemoteWriteRunning = false;
                }
            });
        }

        protected abstract bool TooLarge();

        public abstract Task WriteToRemoteAsync(CancellationToken cancellationToken);

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            misc += GetType().Name + " - local size:  " + SizeDescription + Environment.NewLine;

            return restart;
        }
    }
}

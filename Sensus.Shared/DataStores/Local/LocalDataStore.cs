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

using System;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;
using Sensus.UI.UiProperties;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// A <see cref="LocalDataStore"/> accumulates data on the device. Periodically, data are written from 
    /// the <see cref="LocalDataStore"/> to a <see cref="Remote.RemoteDataStore"/> for permanent storage.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private bool _sizeTriggeredRemoteWriteRunning;
        private bool _writeToRemote;

        private readonly object _sizeTriggeredRemoteWriteLocker = new object();

        [JsonIgnore]
        public abstract string SizeDescription { get; }

        [JsonIgnore]
        public abstract bool HasDataToShare { get; }

        /// <summary>
        /// Whether or not to transfer data from the local data store to the remote data store. If disabled, data
        /// will accumulate indefinitely on local storage media. When the protocol is stopped, the user may then (if
        /// able to access the protocol settings) offload the data from the phone (e.g., via email attachment).
        /// </summary>
        /// <value><c>true</c> to write remote; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Write To Remote:", true, 1)]
        public bool WriteToRemote
        {
            get
            {
                return _writeToRemote;
            }
            set
            {
                _writeToRemote = value;
            }
        }


        protected LocalDataStore()
        {
            _sizeTriggeredRemoteWriteRunning = false;
            _writeToRemote = true;
        }

        /// <summary>
        /// Writes a single <see cref="Datum"/> to the <see cref="LocalDataStore"/>.
        /// </summary>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract void WriteDatum(Datum datum, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether the current <see cref="LocalDataStore"/> has grown too large, and (if it has) writes the data to the 
        /// <see cref="Remote.RemoteDataStore"/>. This relieves pressure on local resources in cases where the <see cref="Remote.RemoteDataStore"/> 
        /// is not triggered frequently enough by its own schedule. It makes sense to call this method periodically after writing 
        /// data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected Task WriteToRemoteIfTooLargeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                bool write = false;

                lock (_sizeTriggeredRemoteWriteLocker)
                {
                    if (IsTooLarge() && !_sizeTriggeredRemoteWriteRunning)
                    {
                        _sizeTriggeredRemoteWriteRunning = true;
                        write = true;
                    }
                }

                if (write)
                {
                    try
                    {
                        SensusServiceHelper.Get().Logger.Log("Running size-triggered write to remote.", LoggingLevel.Normal, GetType());

                        Analytics.TrackEvent(TrackedEvent.Health + ":" + GetType().Name, new Dictionary<string, string>
                        {
                            { "Write", "Size Triggered" }
                        });

                        if (!await Protocol.RemoteDataStore.WriteLocalDataStoreAsync(cancellationToken))
                        {
                            throw new Exception("Failed to write local data store to remote.");
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to run size-triggered write to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                    finally
                    {
                        _sizeTriggeredRemoteWriteRunning = false;
                    }
                }
            });
        }

        protected abstract bool IsTooLarge();

        public abstract Task WriteToRemoteAsync(CancellationToken cancellationToken);

        public abstract void CreateTarFromLocalData(string outputPath);

        public Task ShareLocalDataAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    string tarSharePath = SensusServiceHelper.Get().GetSharePath(".tar");
                    CreateTarFromLocalData(tarSharePath);
                    await SensusServiceHelper.Get().ShareFileAsync(tarSharePath, "Data:  " + Protocol.Name, "application/octet-stream");
                }
                catch (Exception ex)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Error sharing data:  " + ex.Message);
                }
            });
        }
    }
}

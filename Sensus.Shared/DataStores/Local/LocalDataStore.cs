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
        protected async Task WriteToRemoteIfTooLargeAsync(CancellationToken cancellationToken)
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
        }

        public async Task ShareLocalDataAsync()
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
        }

        protected abstract bool IsTooLarge();

        public abstract Task WriteToRemoteAsync(CancellationToken cancellationToken);

        public abstract void CreateTarFromLocalData(string outputPath);
    }
}

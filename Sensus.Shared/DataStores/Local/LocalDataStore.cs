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
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// Responsible for transferring data from probes to local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private bool _uploadToRemoteDataStore;
        private bool _sizeTriggeredRemoteCommitRunning;

        private readonly object _sizeTriggeredRemoteCommitLocker = new object();

        [OnOffUiProperty("Upload to Remote:", true, 3)]
        public bool UploadToRemoteDataStore
        {
            get { return _uploadToRemoteDataStore; }
            set { _uploadToRemoteDataStore = value; }
        }

        [JsonIgnore]
        public abstract string SizeDescription { get; }

        protected LocalDataStore()
        {
            _uploadToRemoteDataStore = true;
            _sizeTriggeredRemoteCommitRunning = false;

#if DEBUG || UI_TESTING
            CommitDelayMS = 5000;  // 5 seconds...so we can see debugging output quickly
#else
            CommitDelayMS = 1000 * 60 * 15;  // 15 minutes
#endif
        }

        /// <summary>
        /// Checks whether the current local data store has grown too large, and (if it has) commits the data to the remote data store.
        /// This relieves pressure on local storage resources (e.g., RAM or disk) in cases where the remote data store is not triggered
        /// frequently enough by its own callback delays. It makes sense to call this method after committing data to the current local data
        /// store. This method will have no effect if the local data store is configured to not upload data to the remote data store.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected Task CommitToRemoteIfTooLargeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                bool commit = false;

                lock (_sizeTriggeredRemoteCommitLocker)
                {
                    if (TooLarge() && !_sizeTriggeredRemoteCommitRunning)
                    {
                        _sizeTriggeredRemoteCommitRunning = true;
                        commit = true;
                    }
                }

                try
                {
                    if (commit)
                    {
                        SensusServiceHelper.Get().Logger.Log("Running size-triggered commit to remote.", LoggingLevel.Normal, GetType());
                        await Protocol.RemoteDataStore.CommitAndReleaseAddedDataAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to run size-triggered commit to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _sizeTriggeredRemoteCommitRunning = false;
                }
            });
        }

        protected abstract bool TooLarge();

        public abstract void CommitToRemote(CancellationToken cancellationToken);

        public int WriteDataToZipFile(string zipPath, CancellationToken cancellationToken, Action<string, double> progressCallback)
        {            
            using (FileStream zipFile = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create))
                {
                    string zipArchiveDirectoryName = Protocol.Name + "_Data_" + DateTime.UtcNow.ToShortDateString() + "_" + DateTime.UtcNow.ToShortTimeString();
                    zipArchiveDirectoryName = new Regex("[^a-zA-Z0-9]").Replace(zipArchiveDirectoryName, "_");

                    progressCallback?.Invoke("Writing data...", 0);

                    Dictionary<string, StreamWriter> datumTypeFile = new Dictionary<string, StreamWriter>();
                    int writtenDataCount = 0;
                    foreach (Tuple<string, string> datumTypeLine in GetDataLinesToWrite(cancellationToken, progressCallback))
                    {
                        string datumType = datumTypeLine.Item1;
                        string line = datumTypeLine.Item2;

                        StreamWriter file;
                        if (datumTypeFile.TryGetValue(datumType, out file))
                        {
                            file.WriteLine(",");
                        }
                        else
                        {
                            ZipArchiveEntry zipFileEntry = zipArchive.CreateEntry(Path.Combine(zipArchiveDirectoryName, datumType + ".json"), CompressionLevel.Optimal);
                            file = new StreamWriter(zipFileEntry.Open());
                            file.WriteLine("[");
                            datumTypeFile.Add(datumType, file);
                        }

                        file.Write(line);
                        ++writtenDataCount;
                    }

                    foreach (StreamWriter file in datumTypeFile.Values)
                    {
                        file.Write(Environment.NewLine + "]");
                        file.Close();
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    progressCallback?.Invoke(null, 1);

                    return writtenDataCount;
                }
            }
        }

        protected abstract IEnumerable<Tuple<string, string>> GetDataLinesToWrite(CancellationToken cancellationToken, Action<string, double> progressCallback);

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            misc += GetType().Name + " - local size:  " + SizeDescription + Environment.NewLine;

            return restart;
        }
    }
}

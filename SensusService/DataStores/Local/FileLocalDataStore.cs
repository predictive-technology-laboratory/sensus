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
using SensusService.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SensusService.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        private string _path;

        private readonly object _storageDirectoryLocker = new object();
        private readonly object _commitToRemoteLocker = new object();

        [JsonIgnore]
        public string StorageDirectory
        {
            get
            {
                string directory = Path.Combine(Protocol.StorageDirectory, GetType().FullName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get { return "File"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        public override void Start()
        {
            // file needs to be ready to accept data immediately, so set file path before calling base.Start
            WriteToNewPath();

            base.Start();
        }

        public override Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    lock (_storageDirectoryLocker)
                    {
                        List<Datum> committedData = new List<Datum>();

                        using (StreamWriter file = new StreamWriter(_path, true))
                        {
                            foreach (Datum datum in data)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;

                                // get JSON for datum
                                string datumJSON = null;
                                try
                                {
                                    datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to get JSON for datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                                }

                                // write JSON to file
                                if (datumJSON != null)
                                {
                                    try
                                    {
                                        file.WriteLine(datumJSON);
                                        MostRecentSuccessfulCommitTime = DateTime.Now;
                                        committedData.Add(datum);
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Failed to write datum JSON to local file:  " + ex.Message, LoggingLevel.Normal, GetType());

                                        // something went wrong with file write...switch to a new file in the hope that it will work better
                                        try
                                        {
                                            WriteToNewPath();
                                            SensusServiceHelper.Get().Logger.Log("Initialized new local file.", LoggingLevel.Normal, GetType());
                                        }
                                        catch (Exception ex2)
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Failed to initialize new file after failing to write the old one:  " + ex2.Message, LoggingLevel.Normal, GetType());
                                        }
                                    }
                                }
                            }
                        }

                        return committedData;
                    }
                });
        }

        public override Task CommitDataToRemoteDataStore(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                // get file paths to commit, as well as a special path to use for uncommitted data.
                string[] paths;
                string uncommittedDataPath;
                lock (_storageDirectoryLocker)
                {
                    paths = Directory.GetFiles(StorageDirectory);

                    WriteToNewPath();
                    uncommittedDataPath = _path;

                    WriteToNewPath();
                }

                // lock any other threads out of the commit, since the paths we're going to use could potentially be used by them.
                lock (_commitToRemoteLocker)
                {
                    using (StreamWriter uncommittedDataFile = new StreamWriter(uncommittedDataPath))
                    {
                        // commit data in small batches
                        HashSet<Datum> dataToCommit = new HashSet<Datum>();

                        foreach (string path in paths)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            // if this method is called concurrently, a previous call might have already processed the current path
                            // and deleted it while we were waiting to acquire the commit lock. ensure that the file still exists.
                            if (File.Exists(path))
                            {
                                using (StreamReader file = new StreamReader(path))
                                {
                                    string datumJSON;
                                    while ((datumJSON = file.ReadLine()) != null)
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                            break;

                                        dataToCommit.Add(Datum.FromJSON(datumJSON));

                                        if (dataToCommit.Count >= 10000)
                                            CommitChunksAsync(dataToCommit, 1000, Protocol.RemoteDataStore, cancellationToken).Wait();

                                        // any leftover data should be dumped to the uncommitted file, which will be picked up on next commit.
                                        foreach (Datum datum in dataToCommit)
                                            uncommittedDataFile.WriteLine(datum.GetJSON(Protocol.JsonAnonymizer, false));
                                    }

                                    // if we were canceled, the current file was probably in a state of partial commit. dump the rest of the
                                    // file into the uncommitted data file.
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        string line;
                                        while ((line = file.ReadLine()) != null)
                                            uncommittedDataFile.WriteLine(line);
                                    }
                                }

                                File.Delete(path);
                            }
                        }
                    }
                }
            });
        }

        protected override IEnumerable<Tuple<string, string>> GetDataLinesToWrite(CancellationToken cancellationToken, Action<string, double> progressCallback)
        {
            lock (_storageDirectoryLocker)
            {
                // "$type":"SensusService.Probes.Movement.AccelerometerDatum, SensusiOS"
                Regex datumTypeRegex = new Regex(@"""\$type""\s*:\s*""(?<type>[^,]+),");

                double storageDirectoryMbToRead = SensusServiceHelper.GetDirectorySizeMB(StorageDirectory);
                double storageDirectoryMbRead = 0;

                string[] localPaths = Directory.GetFiles(StorageDirectory);
                for (int localPathNum = 0; localPathNum < localPaths.Length; ++localPathNum)
                {
                    string localPath = localPaths[localPathNum];

                    using (StreamReader localFile = new StreamReader(localPath))
                    {
                        long localFilePosition = 0;

                        string line;
                        while ((line = localFile.ReadLine()) != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            string type = datumTypeRegex.Match(line).Groups["type"].Value;
                            type = type.Substring(type.LastIndexOf('.') + 1);

                            yield return new Tuple<string, string>(type, line);

                            if (localFile.BaseStream.Position > localFilePosition)
                            {
                                int oldMbRead = (int)storageDirectoryMbRead;
                                storageDirectoryMbRead += (localFile.BaseStream.Position - localFilePosition) / (1024d * 1024d);
                                int newMbRead = (int)storageDirectoryMbRead;

                                if (newMbRead > oldMbRead && progressCallback != null && storageDirectoryMbToRead > 0)
                                    progressCallback(null, storageDirectoryMbRead / storageDirectoryMbToRead);

                                localFilePosition = localFile.BaseStream.Position;
                            }
                        }
                    }
                }

                if (progressCallback != null)
                    progressCallback(null, 1);
            }
        }

        private void WriteToNewPath()
        {
            lock (_storageDirectoryLocker)
            {
                _path = null;
                int pathNumber = 0;
                while (pathNumber++ < int.MaxValue && _path == null)
                {
                    try
                    {
                        _path = Path.Combine(StorageDirectory, pathNumber.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw new SensusException("Failed to get path to local file:  " + ex.Message, ex);
                    }

                    if (File.Exists(_path))
                        _path = null;
                }

                if (_path == null)
                    throw new SensusException("Failed to find new path.");
            }
        }

        public override void Clear()
        {
            base.Clear();

            lock (_storageDirectoryLocker)
            {
                if (Protocol != null)
                {
                    foreach (string path in Directory.GetFiles(StorageDirectory))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to delete local file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                }
            }
        }

        public override void ClearForSharing()
        {
            base.ClearForSharing();

            _path = null;
        }
    }
}
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
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Sensus.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        private const double REMOTE_COMMIT_TRIGGER_STORAGE_DIRECTORY_SIZE_MB = 10;
        private const double MAX_FILE_SIZE_MB = 1;

        private string _path;

        private readonly object _storageDirectoryLocker = new object();

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

        [JsonIgnore]
        public override string SizeDescription
        {
            get
            {
                string desc = null;

                try
                {
                    desc = Math.Round(SensusServiceHelper.GetDirectorySizeMB(StorageDirectory), 1) + " MB";
                }
                catch (Exception)
                {
                }

                return desc;
            }
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
                List<Datum> committedData = new List<Datum>();

                lock (_storageDirectoryLocker)
                {
                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to write data:  " + ex.Message, LoggingLevel.Normal, GetType());

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

                    // switch to a new path if the current one has grown too large
                    if (SensusServiceHelper.GetFileSizeMB(_path) >= MAX_FILE_SIZE_MB)
                        WriteToNewPath();
                }

                CommitToRemoteIfTooLarge(cancellationToken);

                return committedData;
            });
        }

        public override void CommitToRemote(CancellationToken cancellationToken)
        {
            lock (_storageDirectoryLocker)
            {
                string[] pathsToCommit = Directory.GetFiles(StorageDirectory);

                // get path for uncommitted data
                WriteToNewPath();
                string uncommittedDataPath = _path;

                // reset _path for standard commits
                WriteToNewPath();

                using (StreamWriter uncommittedDataFile = new StreamWriter(uncommittedDataPath))
                {
                    foreach (string pathToCommit in pathsToCommit)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // wrap in try-catch to ensure that we process all files
                        try
                        {
                            using (StreamReader fileToCommit = new StreamReader(pathToCommit))
                            {
                                // commit data in small batches. all data will end up in the uncommitted file, in the batch object, or
                                // in the remote data store.
                                HashSet<Datum> batch = new HashSet<Datum>();
                                string datumJSON;
                                while ((datumJSON = fileToCommit.ReadLine()) != null)
                                {
                                    // if we have been canceled, dump the rest of the file into the uncommitted data file.
                                    if (cancellationToken.IsCancellationRequested)
                                        uncommittedDataFile.WriteLine(datumJSON);
                                    else
                                    {
                                        // wrap in try-catch to ensure that we process all lines
                                        try
                                        {
                                            batch.Add(Datum.FromJSON(datumJSON));
                                        }
                                        catch (Exception ex)
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Failed to add datum to batch from JSON:  " + ex.Message, LoggingLevel.Normal, GetType());
                                        }

                                        if (batch.Count >= 50000)
                                            CommitBatchToRemoteAndRelease(batch, cancellationToken, uncommittedDataFile);
                                    }
                                }

                                // commit partial batch
                                if (batch.Count > 0)
                                {
                                    CommitBatchToRemoteAndRelease(batch, cancellationToken, uncommittedDataFile);
                                }
                            }

                            // we've read all lines in the file and either committed them to the remote data store or written them
                            // to the uncommitted data file. we can delete the current path.
                            File.Delete(pathToCommit);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to commit:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                }
            }
        }

        private void CommitBatchToRemoteAndRelease(HashSet<Datum> batch, CancellationToken cancellationToken, StreamWriter uncommittedDataFile)
        {
            if (!cancellationToken.IsCancellationRequested)
                CommitAndReleaseAsync(batch, Protocol.RemoteDataStore, cancellationToken).Wait();

            // any leftover data should be dumped to the uncommitted file to maintain memory limits. the data will be committed next time.
            foreach (Datum datum in batch)
                uncommittedDataFile.WriteLine(datum.GetJSON(Protocol.JsonAnonymizer, false));

            // all data were either commmitted or dumped to the uncommitted file. clear the batch.
            batch.Clear();
        }

        protected override bool TooLarge()
        {
            lock (_storageDirectoryLocker)
            {
                return SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) >= REMOTE_COMMIT_TRIGGER_STORAGE_DIRECTORY_SIZE_MB;
            }
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

                    // create an empty file at the path if one does not exist
                    if (File.Exists(_path))
                        _path = null;
                    else
                        File.Create(_path).Dispose();
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

        public override void Reset()
        {
            base.Reset();

            _path = null;
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            lock (_storageDirectoryLocker)
            {
                int fileCount = Directory.GetFiles(StorageDirectory).Length;

                string name = GetType().Name;
                misc += "Number of files (" + name + "):  " + fileCount + Environment.NewLine +
                        "Average file size (MB) (" + name + "):  " + Math.Round(SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) / (float)fileCount, 2) + Environment.NewLine;
            }

            return restart;
        }
    }
}
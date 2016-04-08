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
using SensusUI.UiProperties;
using System.Threading.Tasks;

namespace SensusService.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        private string _path;

        private readonly object _locker = new object();

        private string StorageDirectory
        {
            get
            {
                string directory = Path.Combine(Protocol.StorageDirectory, GetType().FullName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        public override string DisplayName
        {
            get { return "File"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        public FileLocalDataStore()
        {
        }

        public override void Start()
        {            
            lock (_locker)
            {
                // file needs to be ready to accept data immediately, so set file path before calling base.Start
                WriteToNewPath();

                base.Start();
            }
        }

        protected override Task<List<Datum>> CommitDataAsync(List<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    lock (_locker)
                    {
                        List<Datum> committedData = new List<Datum>();

                        using (StreamWriter file = new StreamWriter(_path, true))
                        {
                            foreach (Datum datum in data)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;
                    
                                string datumJSON = null;
                                try
                                {
                                    datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to get JSON for datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                                }

                                if (datumJSON != null)
                                {
                                    bool writtenToFile = false;
                                    try
                                    {
                                        file.WriteLine(datumJSON);
                                        writtenToFile = true;
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

                                    if (writtenToFile)
                                        committedData.Add(datum);
                                }                        
                            }
                        }

                        return committedData;
                    
                    }
                });
        }

        public override List<Datum> GetDataForRemoteDataStore(CancellationToken cancellationToken, Action<double> progressCallback)
        {
            lock (_locker)
            {
                List<Datum> localData = new List<Datum>();

                string[] paths = Directory.GetFiles(StorageDirectory);
                for (int pathNum = 0; pathNum < paths.Length; ++pathNum)
                {   
                    string path = paths[pathNum];

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (progressCallback != null)
                        progressCallback(pathNum / (double)paths.Length);
                    
                    try
                    {
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while (!cancellationToken.IsCancellationRequested && !string.IsNullOrWhiteSpace(line = file.ReadLine()))
                                localData.Add(Datum.FromJSON(line));
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while reading local data store for transfer to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }                         
                }

                if (cancellationToken.IsCancellationRequested)
                    SensusServiceHelper.Get().Logger.Log("Canceled retrieval of local data for remote data store.", LoggingLevel.Normal, GetType());

                // start writing to new path if we're still running
                if (Running)
                    WriteToNewPath();

                return localData;
            }
        }

        public override void ClearDataCommittedToRemoteDataStore(List<Datum> dataCommittedToRemote)
        {
            lock (_locker)
            {
                SensusServiceHelper.Get().Logger.Log("Received " + dataCommittedToRemote.Count + " remote-committed data elements to clear.", LoggingLevel.Normal, GetType());

                HashSet<Datum> hashDataCommittedToRemote = new HashSet<Datum>(dataCommittedToRemote);  // for quick access via hashing

                // clear remote-committed data from all files
                foreach (string path in Directory.GetFiles(StorageDirectory))
                {
                    SensusServiceHelper.Get().Logger.Log("Clearing remote-committed data from \"" + path + "\".", LoggingLevel.Debug, GetType());

                    string uncommittedDataPath = Path.GetTempFileName();
                    int uncommittedDataCount = 0;
                    using (StreamWriter uncommittedDataFile = new StreamWriter(uncommittedDataPath))
                    {
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while ((line = file.ReadLine()) != null)
                            {
                                Datum datum = Datum.FromJSON(line);
                                if (!hashDataCommittedToRemote.Contains(datum))
                                {
                                    uncommittedDataFile.WriteLine(datum.GetJSON(Protocol.JsonAnonymizer, false));  // need to pass in the anonymizer, since the user might have selected an anonymization option between the time that the datum was written to file and the time of execution of the current line of code.
                                    ++uncommittedDataCount;
                                }
                            }
                        }
                    }

                    File.Delete(path);

                    // if there were no uncommitted data in the file, the uncommitted data file will be empty -- delete it
                    if (uncommittedDataCount == 0)
                    {
                        SensusServiceHelper.Get().Logger.Log("Cleared all data from local file. Deleting file.", LoggingLevel.Debug, GetType());
                        File.Delete(uncommittedDataPath);
                    }
                    // if there were uncommitted data in the file, replace it with the file holding the uncommitted data -- it will be committed next time
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log(uncommittedDataCount + " data elements in local file were not committed to remote data store.", LoggingLevel.Debug, GetType());
                        File.Move(uncommittedDataPath, path);
                    }
                }

                // reinitialize file if we're running
                if (Running)
                    WriteToNewPath();

                SensusServiceHelper.Get().Logger.Log("Finished clearing remote-committed data elements.", LoggingLevel.Normal, GetType());
            }
        }

        private void WriteToNewPath()
        {
            lock (_locker)
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
                        throw new DataStoreException("Failed to get path to local file:  " + ex.Message);
                    }

                    if (File.Exists(_path))
                        _path = null;
                }

                if (_path == null)
                    throw new DataStoreException("Failed to find new path.");
            }
        }

        public override void Clear()
        {    
            lock (_locker)
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
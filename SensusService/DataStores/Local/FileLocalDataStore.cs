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

namespace SensusService.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        private StreamWriter _file;
        private string _path;
        private int _numDataStoredInFiles;

        private readonly object _locker = new object();

        public int NumDataStoredInFiles
        {
            get
            {
                return _numDataStoredInFiles;
            }
            set
            {
                _numDataStoredInFiles = value;
            }
        }           

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

        protected override string DisplayName
        {
            get { return "File"; }
        }

        public override int DataCount
        {
            get
            {
                return _numDataStoredInFiles;
            }
        }
        
        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        public FileLocalDataStore()
        {
            _numDataStoredInFiles = 0;
        }

        public override void Start()
        {
            // file needs to be ready to accept data immediately
            lock (_locker)
            {
                InitializeFile();

                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data, CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                List<Datum> committedData = new List<Datum>();

                foreach (Datum datum in data)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    string datumJSON = null;
                    try { datumJSON = datum.GetJSON(Protocol.JsonAnonymizer); }
                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to get JSON for datum:  " + ex.Message, LoggingLevel.Normal, GetType()); }

                    if (datumJSON != null)
                    {
                        bool writtenToFile = false;
                        try
                        {
                            _file.WriteLine(datumJSON);
                            writtenToFile = true;
                            ++_numDataStoredInFiles;
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to write datum JSON to local file:  " + ex.Message, LoggingLevel.Normal, GetType());

                            try
                            {
                                InitializeFile();

                                SensusServiceHelper.Get().Logger.Log("Initialized new local file.", LoggingLevel.Normal, GetType());
                            }
                            catch (Exception ex2) { SensusServiceHelper.Get().Logger.Log("Failed to initialize new file after failing to write the old one:  " + ex2.Message, LoggingLevel.Normal, GetType()); }
                        }

                        if (writtenToFile)
                            committedData.Add(datum);
                    }                        
                }

                return committedData;
            }
        }

        public override List<Datum> GetDataForRemoteDataStore(CancellationToken cancellationToken, Action<double> progressCallback)
        {
            lock (_locker)
            {
                CloseFile();

                // get local data from all files
                List<Datum> localData = new List<Datum>();
                foreach (string path in Directory.GetFiles(StorageDirectory))
                {   
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    try
                    {
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while (!cancellationToken.IsCancellationRequested && (line = file.ReadLine()) != null)
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    localData.Add(Datum.FromJSON(line));

                                    if(progressCallback != null && _numDataStoredInFiles >= 10 && (localData.Count % (_numDataStoredInFiles / 10)) == 0)
                                        progressCallback(localData.Count / (double)_numDataStoredInFiles);
                                }

                            file.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while reading local data store for transfer to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }                         
                }

                if (cancellationToken.IsCancellationRequested)
                    SensusServiceHelper.Get().Logger.Log("Canceled retrieval of local data for remote data store.", LoggingLevel.Normal, GetType());

                // reinitialize file if we're running
                if (Running)
                    InitializeFile();

                return localData;
            }
        }

        public override void ClearDataCommittedToRemoteDataStore(List<Datum> dataCommittedToRemote)
        {
            lock (_locker)
            {
                CloseFile();

                SensusServiceHelper.Get().Logger.Log("Received " + dataCommittedToRemote.Count + " remote-committed data elements to clear.", LoggingLevel.Normal, GetType());

                HashSet<Datum> hashDataCommittedToRemote = new HashSet<Datum>(dataCommittedToRemote);  // for quick access via hashing

                // clear remote-committed data from all local files
                foreach (string path in Directory.GetFiles(StorageDirectory))
                {
                    SensusServiceHelper.Get().Logger.Log("Clearing remote-committed data from \"" + path + "\".", LoggingLevel.Debug, GetType());

                    string uncommittedDataPath = Path.GetTempFileName();
                    int uncommittedDataCount = 0;
                    using (StreamWriter uncommittedDataFile = new StreamWriter(uncommittedDataPath))
                    using (StreamReader file = new StreamReader(path))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            Datum datum = Datum.FromJSON(line);
                            if (hashDataCommittedToRemote.Contains(datum))
                                --_numDataStoredInFiles;
                            else
                            {
                                uncommittedDataFile.WriteLine(datum.GetJSON(Protocol.JsonAnonymizer));  // need to pass in the anonymizer, since the user might have selected an anonymization option between the time that the datum was written to file and the time of execution of the current line of code.
                                ++uncommittedDataCount;
                            }
                        }

                        uncommittedDataFile.Close();
                        file.Close();
                    }

                    if (uncommittedDataCount == 0)  // all data in local file were committed to remote data store -- delete local and filtered files
                    {
                        SensusServiceHelper.Get().Logger.Log("Cleared all data from local file. Deleting file.", LoggingLevel.Debug, GetType());

                        File.Delete(path);
                        File.Delete(uncommittedDataPath);
                    }
                    else  // data from local file were not committed to the remote data store -- move filtered path to local path and retry sending to remote store next time
                    {
                        SensusServiceHelper.Get().Logger.Log(uncommittedDataCount + " data elements in local file were not committed to remote data store.", LoggingLevel.Debug, GetType());

                        File.Delete(path);
                        File.Move(uncommittedDataPath, path);
                    }
                }

                // reinitialize file if we're running
                if (Running)
                    InitializeFile();

                SensusServiceHelper.Get().Logger.Log("Finished clearing remote-committed data elements.", LoggingLevel.Normal, GetType());
            }
        }

        public override void Stop()
        {
            lock (_locker)
            {
                // stop the commit thread
                base.Stop();

                // close current file -- don't clear the files out. the user can clear them or they can be uploaded to remote.
                CloseFile();
            }
        }

        /// <summary>
        /// Initializes a new file. Should be called from a locked context.
        /// </summary>
        private void InitializeFile()
        {
            CloseFile();

            try
            {
                for (int i = 0; _file == null && i < int.MaxValue; ++i)
                {
                    try { _path = Path.Combine(StorageDirectory, i.ToString()); }  // getting the storage directory creates the directory, which could fail
                    catch (Exception ex) { throw new DataStoreException("Failed to get path to local file:  " + ex.Message); }

                    if (!File.Exists(_path))
                    {
                        try
                        {
                            _file = new StreamWriter(_path);
                            _file.AutoFlush = true;
                        }
                        catch (Exception ex) { throw new DataStoreException("Failed to open local file at a path that did not previously exist:  " + ex.Message); }
                    }
                }

                if (_file == null)
                    throw new DataStoreException("Failed to open file.");
            }
            catch (Exception ex)
            {
                _file = null;
                _path = null;
                throw new DataStoreException("Failed to initialize new file:  " + ex.Message);
            }
        }

        /// <summary>
        /// Closes the current file. Should be called from a locked context.
        /// </summary>
        private void CloseFile()
        {
            if (_file != null)
            {
                try { _file.Close(); }
                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to close file:  " + ex.Message, LoggingLevel.Normal, GetType()); }

                _file = null;
            }

            _path = null;
        }

        public override void Clear()
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

                _numDataStoredInFiles = 0;

                SensusServiceHelper.Get().SaveAsync();  // update num data stored within the JSON file
            }
        }
    }
}

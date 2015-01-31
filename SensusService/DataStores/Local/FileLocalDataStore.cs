#region copyright
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
#endregion

using Newtonsoft.Json;
using SensusService.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SensusService.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        /// <summary>
        /// File being written with local data.
        /// </summary>
        private StreamWriter _file;

        /// <summary>
        /// Contains the path to the file currently being written.
        /// </summary>
        private string _path;

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
        
        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        public override void Start()
        {
            // file needs to be ready to accept data immediately
            lock (this)
            {
                InitializeFile();

                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data)
        {
            lock (this)
            {
                List<Datum> committedData = new List<Datum>();

                foreach (Datum datum in data)
                {
                    string datumJSON = null;
                    try { datumJSON = datum.JSON; }
                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to get JSON for datum:  " + ex.Message, LoggingLevel.Normal); }

                    if (datumJSON != null)
                    {
                        bool writtenToFile = false;
                        try
                        {
                            _file.WriteLine(datumJSON);
                            writtenToFile = true;
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to write datum JSON to local file:  " + ex.Message, LoggingLevel.Normal);

                            try
                            {
                                InitializeFile();

                                SensusServiceHelper.Get().Logger.Log("Initialized new local file.", LoggingLevel.Normal);
                            }
                            catch (Exception ex2) { SensusServiceHelper.Get().Logger.Log("Failed to initialize new local data store file after failing to write the old one:  " + ex2.Message, LoggingLevel.Normal); }
                        }

                        if (writtenToFile)
                            committedData.Add(datum);
                    }
                }

                return committedData;
            }
        }

        public override List<Datum> GetDataForRemoteDataStore()
        {
            lock (this)
            {
                CloseFile();

                // get local data from all files
                List<Datum> localData = new List<Datum>();
                foreach (string path in Directory.GetFiles(StorageDirectory))
                    try
                    {
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while ((line = file.ReadLine()) != null)
                                if (!string.IsNullOrWhiteSpace(line))
                                    localData.Add(Datum.FromJSON(line));

                            file.Close();
                        }
                    }
                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Exception while reading local data store for transfer to remote:  " + ex.Message, LoggingLevel.Normal); }

                // this method is called from a remote data store, which doesn't care whether this local data store is running. don't start a new file unless this local data store is, in fact, running.
                if (Running)
                    InitializeFile();

                return localData;
            }
        }

        public override void ClearDataCommittedToRemoteDataStore(List<Datum> dataCommittedToRemote)
        {
            lock (this)
            {
                SensusServiceHelper.Get().Logger.Log("Local data store received " + dataCommittedToRemote.Count + " remote-committed elements to clear.", LoggingLevel.Verbose);

                HashSet<Datum> hashDataCommittedToRemote = new HashSet<Datum>(dataCommittedToRemote);  // for quick access via hashing

                foreach (string path in Directory.GetFiles(StorageDirectory))
                    if (path != _path)  // don't process the file that's currently being written
                    {
                        SensusServiceHelper.Get().Logger.Log("Clearing remote-committed data from \"" + path + "\".", LoggingLevel.Verbose);

                        string filteredPath = Path.GetTempFileName();
                        int filteredDataWritten = 0;
                        using (StreamWriter filteredFile = new StreamWriter(filteredPath))
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while ((line = file.ReadLine()) != null)
                            {
                                Datum datum = Datum.FromJSON(line);
                                if (!hashDataCommittedToRemote.Contains(datum))
                                {
                                    filteredFile.WriteLine(datum.JSON);
                                    filteredDataWritten++;
                                }
                            }

                            filteredFile.Close();
                            file.Close();
                        }

                        if (filteredDataWritten == 0)  // all data in local file were committed to remote data store -- delete local and filtered files
                        {
                            SensusServiceHelper.Get().Logger.Log("Cleared all data from local data store file. Deleting file.", LoggingLevel.Verbose);

                            File.Delete(path);
                            File.Delete(filteredPath);
                        }
                        else  // data from local file were not committed to the remote data store -- move filtered path to local path and retry sending to remote store next time
                        {
                            SensusServiceHelper.Get().Logger.Log(filteredDataWritten + " data elements in local data store file were not committed to remote data store.", LoggingLevel.Normal);

                            File.Delete(path);
                            File.Move(filteredPath, path);
                        }
                    }
            }
        }

        public override void Stop()
        {
            lock (this)
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
                    catch (Exception ex) { throw new DataStoreException("Failed to get path to local data file:  " + ex.Message); }

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
                    throw new DataStoreException("Failed to open file for local data store.");
            }
            catch (Exception ex)
            {
                _file = null;
                _path = null;
                throw new SensusException("Failed to initialize new local data store file:  " + ex.Message);
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
                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to close file for local data store:  " + ex.Message, LoggingLevel.Normal); }

                _file = null;
            }

            _path = null;
        }

        public override void Clear()
        {
            if (Protocol != null)
                foreach (string path in Directory.GetFiles(StorageDirectory))
                    try { File.Delete(path); }
                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to delete local data store file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal); }
        }
    }
}

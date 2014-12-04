using Newtonsoft.Json;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.DataStores.Local
{
    public class FileLocalDataStore : LocalDataStore
    {
        private StreamWriter _file;
        private string _path;
        private JsonSerializerSettings _serializationSettings;

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
        public override bool CanClear
        {
            get { return true; }
        }

        public FileLocalDataStore()
        {
            _serializationSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };
        }

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    // file needs to be ready to accept data immediately
                    lock (this)
                        InitializeFile();

                    await base.StartAsync();
                });
        }

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            return Task.Run<ICollection<Datum>>(() =>
                {
                    lock (this)
                    {
                        List<Datum> committedData = new List<Datum>();

                        foreach (Datum datum in data)
                        {
                            string datumJSON = null;
                            try { datumJSON = GetJSON(datum); }
                            catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to get JSON for datum:  " + ex.Message); }

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
                                    if (App.LoggingLevel >= LoggingLevel.Normal)
                                        App.Get().SensusService.Log("Failed to write datum JSON to local file:  " + ex.Message);

                                    try
                                    {
                                        InitializeFile();

                                        if (App.LoggingLevel >= LoggingLevel.Normal)
                                            App.Get().SensusService.Log("Initialized new local file.");
                                    }
                                    catch (Exception ex2) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to initialize new local data store file after failing to write the old one:  " + ex2.Message); }
                                }

                                if (writtenToFile)
                                    committedData.Add(datum);
                            }
                        }

                        return committedData;
                    }
                });
        }

        public override ICollection<Datum> GetDataForRemoteDataStore()
        {
            lock (this)
            {
                bool initializeNewFile = false;
                if (_file != null)
                {
                    try { _file.Close(); }
                    catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to close current local data store file:  " + ex.Message); }

                    _file = null;
                    initializeNewFile = true;
                }

                List<Datum> localData = new List<Datum>();

                foreach (string path in Directory.GetFiles(StorageDirectory))
                    try
                    {
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while ((line = file.ReadLine()) != null)
                                localData.Add(JsonConvert.DeserializeObject<Datum>(line, _serializationSettings));

                            file.Close();
                        }
                    }
                    catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Exception while reading local data store for transfer to remote:  " + ex.Message); }

                if (initializeNewFile)
                    InitializeFile();

                return localData;
            }
        }

        public override void ClearDataCommittedToRemoteDataStore(ICollection<Datum> dataCommittedToRemote)
        {
            lock (this)
            {
                if (App.LoggingLevel >= LoggingLevel.Verbose)
                    App.Get().SensusService.Log("Local data store received " + dataCommittedToRemote.Count + " remote-committed elements to clear.");

                HashSet<Datum> hashDataCommittedToRemote = new HashSet<Datum>(dataCommittedToRemote);  // for quick access via hashing

                foreach (string path in Directory.GetFiles(StorageDirectory))
                    if (path != _path)  // don't process the file that's currently being written
                    {
                        if (App.LoggingLevel >= LoggingLevel.Verbose)
                            App.Get().SensusService.Log("Clearing remote-committed data from \"" + path + "\".");

                        string filteredPath = Path.GetTempFileName();
                        int filteredDataWritten = 0;
                        using (StreamWriter filteredFile = new StreamWriter(filteredPath))
                        using (StreamReader file = new StreamReader(path))
                        {
                            string line;
                            while ((line = file.ReadLine()) != null)
                            {
                                Datum datum = JsonConvert.DeserializeObject<Datum>(line, _serializationSettings);
                                if (!hashDataCommittedToRemote.Contains(datum))
                                {
                                    filteredFile.WriteLine(GetJSON(datum));
                                    filteredDataWritten++;
                                }
                            }

                            filteredFile.Close();
                            file.Close();
                        }

                        if (filteredDataWritten == 0)  // all data in local file were committed to remote data store -- delete local and filtered files
                        {
                            if (App.LoggingLevel >= LoggingLevel.Verbose)
                                App.Get().SensusService.Log("Cleared all data from local data store file. Deleting file.");

                            File.Delete(path);
                            File.Delete(filteredPath);
                        }
                        else  // data from local file were not committed to the remote data store -- move filtered path to local path and retry sending to remote store next time
                        {
                            if (App.LoggingLevel >= LoggingLevel.Normal)
                                App.Get().SensusService.Log(filteredDataWritten + " data elements in local data store file were not committed to remote data store.");

                            File.Delete(path);
                            File.Move(filteredPath, path);
                        }
                    }
            }
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // stop the data store before closing the file to make sure all data are allowed in
                    await base.StopAsync();

                    lock (this)
                    {
                        try
                        {
                            _file.Close();
                            _file = null;
                        }
                        catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to close local data store file:  " + ex.Message); }
                    }
                });
        }

        private string GetJSON(Datum datum)
        {
            return JsonConvert.SerializeObject(datum, Formatting.None, _serializationSettings).Replace('\n', ' ').Replace('\r', ' ');
        }

        private void InitializeFile()
        {
            if (_file != null)
            {
                try { _file.Close(); }
                catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to close file for local data store:  " + ex.Message); }

                _file = null;
            }

            for (int i = 0; _file == null && i < int.MaxValue; ++i)
            {
                _path = Path.Combine(StorageDirectory, i.ToString());
                if (!File.Exists(_path))
                {
                    _file = new StreamWriter(_path);
                    _file.AutoFlush = true;
                }
            }

            if (_file == null)
                throw new DataStoreException("Failed to open file for local data store.");
        }

        public override void Clear()
        {
            foreach (string path in Directory.GetFiles(StorageDirectory))
                try { File.Delete(path); }
                catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to delete local data store file \"" + path + "\":  " + ex.Message); }
        }
    }
}

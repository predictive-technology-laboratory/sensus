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

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StartAsync();

                    string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), GetType().FullName);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    for (int i = 0; _file == null && i < int.MaxValue; ++i)
                    {
                        string path = Path.Combine(directory, i.ToString());
                        if (!File.Exists(path))
                        {
                            _file = new StreamWriter(path);
                            _file.AutoFlush = true;
                        }
                    }

                    if (_file == null)
                        throw new SensusException("Failed to open file for local data store.");
                });
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StopAsync();

                    if (_file != null)
                        lock (_file)
                            try { _file.Close(); }
                            catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to close local data store file:  " + ex.Message); }
                });
        }

        public override ICollection<Datum> GetDataForRemoteDataStore()
        {
            throw new NotImplementedException();
        }

        public override void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }

        protected override string DisplayName
        {
            get { throw new NotImplementedException(); }
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            if (_file == null)
                throw new SensusException("Backing file for local data store is not open.");

            List<Datum> committedData = new List<Datum>();

            JsonSerializerSettings serializationSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };

            foreach (Datum datum in data)
                try
                {
                    _file.WriteLine(JsonConvert.SerializeObject(datum, Formatting.None, serializationSettings));
                    committedData.Add(datum);
                }
                catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to store datum in local file:  " + ex.Message); }

            return committedData;
        }
    }
}

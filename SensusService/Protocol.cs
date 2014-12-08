using Newtonsoft.Json;
using SensusService;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusService.Probes;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SensusService
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
        public enum ShareMethod
        {
            Email,
            Text
        }

        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Task<Protocol> GetFromWeb(string uri)
        {
            return Task.Run(async () =>
                {
                    WebClient client = new WebClient();
                    using (StreamReader reader = new StreamReader(await client.OpenReadTaskAsync(new Uri(uri))))
                    {
                        string protocolJSON = reader.ReadToEnd();
                        reader.Close();
                        return JsonConvert.DeserializeObject<Protocol>(protocolJSON, _jsonSerializerSettings);
                    }
                });
        }

        public static Task<Protocol> GetFromFile(string path)
        {
            return Task.Run(() =>
                {
                    using (StreamReader file = new StreamReader(path))
                    {
                        Protocol protocol = JsonConvert.DeserializeObject<Protocol>(file.ReadToEnd(), _jsonSerializerSettings);
                        file.Close();
                        return protocol;
                    }
                });
        }

        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private int _userId;
        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [EntryIntegerUiProperty("User ID:", false, 3)]
        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Probe> Probes
        {
            get { return _probes; }
            set { _probes = value; }
        }

        [OnOffUiProperty("Status:", true, 2)]
        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
            set
            {
                if (value != _running)
                {
                    _running = value;
                    OnPropertyChanged();

                    if (_running)
                        SensusServiceHelper.Get().StartProtocolAsync(this);
                    else
                        SensusServiceHelper.Get().StopProtocolAsync(this, false);  // don't unregister the protocol when stopped via UI interaction
                }
            }
        }

        public LocalDataStore LocalDataStore
        {
            get { return _localDataStore; }
            set
            {
                if (value != _localDataStore)
                {
                    _localDataStore = value;
                    OnPropertyChanged();

                    _localDataStore.Protocol = this;
                }
            }
        }

        public RemoteDataStore RemoteDataStore
        {
            get { return _remoteDataStore; }
            set
            {
                if (value != _remoteDataStore)
                {
                    _remoteDataStore = value;
                    OnPropertyChanged();

                    _remoteDataStore.Protocol = this;
                }
            }
        }

        [JsonIgnore]
        public string StorageDirectory
        {
            get
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), _id);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        private Protocol() { } // for JSON deserialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userId">Participant's ID. Must be unique within an experiment.</param>
        /// <param name="name">Name of protocol.</param>
        /// <param name="addAllProbes">Whether or not to add all available probes into the protocol.</param>
        public Protocol(int userId, string name, bool addAllProbes)
        {
            _userId = userId;
            _name = name;
            _id = Guid.NewGuid().ToString();
            _running = false;

            _probes = new List<Probe>();

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                    AddProbe(probe);
        }

        public void AddProbe(Probe probe)
        {
            probe.Protocol = this;
            _probes.Add(probe);
        }

        public void RemoveProbe(Probe probe)
        {
            probe.Protocol = null;
            _probes.Remove(probe);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    // if the service is starting this protocol (e.g., when restarting protocols upon startup), then _running/Running will be false here. set it to true to update UI.
                    if (!_running)
                    {
                        _running = true;
                        OnPropertyChanged("Running");
                    }

                    SensusServiceHelper.Get().Logger.Log("Initializing and starting probes for protocol " + _name + ".", LoggingLevel.Normal);

                    int probesStarted = 0;
                    foreach (Probe probe in _probes)
                        if (probe.Enabled && await probe.InitializeAndStartAsync())
                            probesStarted++;

                    if (probesStarted > 0)
                    {
                        try { await _localDataStore.StartAsync(); }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Local data store failed to start:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal);

                            Running = false;
                            return;
                        }

                        try { await _remoteDataStore.StartAsync(); }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Remote data store failed to start:  " + ex.Message, LoggingLevel.Normal);

                            Running = false;
                            return;
                        }
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("No probes were started.", LoggingLevel.Normal);

                        Running = false;
                    }
                });
        }

        public Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // if the service is stopping this protocol, then _running/Running will be true here. set it to false to update UI and allow the data stores to stop.
                    if (_running)
                    {
                        _running = false;
                        OnPropertyChanged("Running");
                    }

                    SensusServiceHelper.Get().Logger.Log("Stopping probes.", LoggingLevel.Normal);

                    foreach (Probe probe in _probes)
                        if (probe.Controller.Running)
                            try { await probe.Controller.StopAsync(); }
                            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to stop " + probe.DisplayName + "'s controller:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }

                    if (_localDataStore != null && _localDataStore.Running)
                    {
                        SensusServiceHelper.Get().Logger.Log("Stopping local data store.", LoggingLevel.Normal);

                        try { await _localDataStore.StopAsync(); }
                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to stop local data store:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }
                    }

                    if (_remoteDataStore != null && _remoteDataStore.Running)
                    {
                        SensusServiceHelper.Get().Logger.Log("Stopping remote data store.", LoggingLevel.Normal);

                        try { await _remoteDataStore.StopAsync(); }
                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to stop remote data store:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }
                    }
                });
        }

        public void Save(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
                file.Close();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Protocol && (obj as Protocol)._id == _id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}

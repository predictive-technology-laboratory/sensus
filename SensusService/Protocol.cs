using Android.Content;
using Newtonsoft.Json;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusService.Exceptions;
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
    /// Container for probes.
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
        #region static members
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Protocol GetFromWeb(Uri uri)
        {
            Stream stream = null;

            try { stream = new WebClient().OpenRead(uri); }
            catch (Exception ex) { throw new SensusException("Failed to open web client to URI \"" + uri + "\":  " + ex.Message + ". If this is an HTTPS URI, make sure the server's certificate is valid."); }

            if (stream == null)
                return null;
            else
                return GetFromStream(stream);
        }

        public static Protocol GetFromFile(Android.Net.Uri uri, ContentResolver contentResolver)
        {
            Stream stream = null;

            try { stream = contentResolver.OpenInputStream(uri); }
            catch (Exception ex) { throw new SensusException("Failed to open local file URI \"" + uri + "\":  " + ex.Message); }

            if (stream == null)
                return null;
            else
                return GetFromStream(stream);
        }

        public static Protocol GetFromStream(Stream stream)
        {
            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    reader.Close();
                    stream.Close();

                    Protocol protocol = JsonConvert.DeserializeObject<Protocol>(json, _jsonSerializerSettings);

                    protocol.StorageDirectory = null;
                    while (protocol.StorageDirectory == null)
                    {
                        protocol.Id = Guid.NewGuid().ToString();
                        string candidateStorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), protocol.Id);
                        if (!Directory.Exists(candidateStorageDirectory))
                        {
                            protocol.StorageDirectory = candidateStorageDirectory;
                            Directory.CreateDirectory(protocol.StorageDirectory);
                        }
                    }

                    return protocol;
                }
            }
            catch (Exception ex) { throw new SensusException("Failed to extract Protocol from stream:  " + ex.Message); }
        }
        #endregion

        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;
        private string _storageDirectory;
        private ProtocolReport _mostRecentReport;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
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
                if (value)
                    SensusServiceHelper.Get().StartProtocolAsync(this);
                else
                    SensusServiceHelper.Get().StopProtocolAsync(this, false);  // don't unregister the protocol when stopped via UI interaction
            }
        }

        /// <summary>
        /// Sets the value of Running for this protocol. This is provided in addition to the set method of Running since the latter results in a call to the 
        /// service helper that starts or stops the protocol. We don't always want that to happen, e.g., if the service is starting the protocol, it won't
        /// want to call Running = true, which would create a recursion.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if value was changed and false otherwise.</returns>
        public bool SetRunning(bool value)
        {
            if (value == _running)
                return false;
            else
            {
                _running = value;
                OnPropertyChanged("Running");
                return true;
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
                    _localDataStore.Protocol = this;

                    OnPropertyChanged();
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
                    _remoteDataStore.Protocol = this;

                    OnPropertyChanged();
                }
            }
        }

        public string StorageDirectory
        {
            get { return _storageDirectory; }
            set { _storageDirectory = value; }
        }

        [JsonIgnore]
        public ProtocolReport MostRecentReport
        {
            get { return _mostRecentReport; }
            set { _mostRecentReport = value; }
        }

        private Protocol() { }  // for JSON deserialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of protocol.</param>
        /// <param name="addAllProbes">Whether or not to add all available probes into the protocol.</param>
        public Protocol(string name, bool addAllProbes)
        {
            _name = name;
            _running = false;

            while (_storageDirectory == null)
            {
                _id = Guid.NewGuid().ToString();
                string candidateStorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), _id);
                if (!Directory.Exists(candidateStorageDirectory))
                {
                    _storageDirectory = candidateStorageDirectory;
                    Directory.CreateDirectory(_storageDirectory);
                }
            }

            _probes = new List<Probe>();

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                {
                    probe.Protocol = this;
                    _probes.Add(probe);
                }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Save(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
                file.Close();
            }
        }

        public Task PingAsync()
        {
            return Task.Run(() => Ping());
        }

        public void Ping()
        {
            lock (this)
            {
                string error = null;
                string warning = null;
                string misc = null;

                if (!_running)
                {
                    error += "Restarting protocol \"" + _name + "\"...";
                    try
                    {
                        SensusServiceHelper.Get().StopProtocol(this, false);
                        SensusServiceHelper.Get().StartProtocol(this);
                    }
                    catch (Exception ex) { error += ex.Message + "..."; }

                    if (_running)
                        error += "restarted protocol." + Environment.NewLine;
                    else
                        error += "failed to restart protocol." + Environment.NewLine;
                }

                if (_running)
                {
                    if (_localDataStore == null)
                        error += "No local data store present on protocol." + Environment.NewLine;
                    else if (_localDataStore.Ping(ref error, ref warning, ref misc))
                    {
                        error += "Restarting local data store...";

                        try { _localDataStore.Restart(); }
                        catch (Exception ex) { error += ex.Message + "..."; }

                        if (!_localDataStore.Running)
                            error += "failed to restart local data store." + Environment.NewLine;
                    }

                    if (_remoteDataStore == null)
                        error += "No remote data store present on protocol." + Environment.NewLine;
                    else if (_remoteDataStore.Ping(ref error, ref warning, ref misc))
                    {
                        error += "Restarting remote data store...";

                        try { _remoteDataStore.Restart(); }
                        catch (Exception ex) { error += ex.Message + "..."; }

                        if (!_remoteDataStore.Running)
                            error += "failed to restart remote data store." + Environment.NewLine;
                    }

                    foreach (Probe probe in _probes)
                        if (probe.Enabled && probe.Ping(ref error, ref warning, ref misc))
                        {
                            error += "Restarting probe \"" + probe.GetType().FullName + "\"...";

                            try { probe.Restart(); }
                            catch (Exception ex) { error += ex.Message + "..."; }

                            if (!probe.Running)
                                error += "failed to restart probe \"" + probe.GetType().FullName + "\"." + Environment.NewLine;
                        }
                }

                _mostRecentReport = new ProtocolReport(DateTimeOffset.UtcNow, error, warning, misc);

                SensusServiceHelper.Get().Logger.Log("Protocol report:" + Environment.NewLine + _mostRecentReport, LoggingLevel.Normal);
            }
        }

        public void UploadMostRecentProtocolReport()
        {
            lock (this)
                if (_mostRecentReport != null)
                {
                    SensusServiceHelper.Get().Logger.Log("Uploading protocol report.", LoggingLevel.Normal);
                    _localDataStore.AddNonProbeDatum(_mostRecentReport);
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

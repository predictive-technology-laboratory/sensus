using Newtonsoft.Json;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sensus
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
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

        [EntryIntegerUiProperty("User ID:", false)]
        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        [StringUiProperty("Name:", true)]
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

        [OnOffUiProperty("Status:", true)]
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
                        App.Get().SensusService.StartProtocolAsync(this);
                    else
                        App.Get().SensusService.StopProtocolAsync(this, false);  // don't unregister the protocol when stopped via UI interaction
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
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), _id); }
        }

        private Protocol() { } // for JSON deserialization

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

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Initializing and starting probes for protocol " + _name + ".");

                    int probesStarted = 0;
                    foreach (Probe probe in _probes)
                        if (probe.Enabled && await probe.InitializeAndStartAsync())
                            probesStarted++;

                    if (probesStarted > 0)
                    {
                        try { await _localDataStore.StartAsync(); }
                        catch (Exception ex)
                        {
                            if (App.LoggingLevel >= LoggingLevel.Normal)
                                App.Get().SensusService.Log("Local data store failed to start:  " + ex.Message + Environment.NewLine + ex.StackTrace);

                            Running = false;
                            return;
                        }

                        try { await _remoteDataStore.StartAsync(); }
                        catch (Exception ex)
                        {
                            if (App.LoggingLevel >= LoggingLevel.Normal)
                                App.Get().SensusService.Log("Remote data store failed to start:  " + ex.Message);

                            Running = false;
                            return;
                        }
                    }
                    else
                    {
                        if (App.LoggingLevel >= LoggingLevel.Normal)
                            App.Get().SensusService.Log("No probes were started.");

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

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Stopping probes.");

                    foreach (Probe probe in _probes)
                        if (probe.Controller.Running)
                            try { await probe.Controller.StopAsync(); }
                            catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to stop " + probe.DisplayName + "'s controller:  " + ex.Message + Environment.NewLine + ex.StackTrace); }

                    if (_localDataStore != null && _localDataStore.Running)
                    {
                        if (App.LoggingLevel >= LoggingLevel.Normal)
                            App.Get().SensusService.Log("Stopping local data store.");

                        try { await _localDataStore.StopAsync(); }
                        catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to stop local data store:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                    }

                    if (_remoteDataStore != null && _remoteDataStore.Running)
                    {
                        if (App.LoggingLevel >= LoggingLevel.Normal)
                            App.Get().SensusService.Log("Stopping remote data store.");

                        try { await _remoteDataStore.StopAsync(); }
                        catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to stop remote data store:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                    }
                });
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

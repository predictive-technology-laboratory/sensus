using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Sensus
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    [Serializable]
    public class Protocol : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private List<Probe> _probes;
        [NonSerialized]
        private bool _running;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;

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
        }

        [BooleanUiProperty("Status:", true)]
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
                        App.Get().SensusService.StartProtocol(this);
                    else
                        App.Get().SensusService.StopProtocol(this, false);  // don't unregister the protocol when stopped via the UI
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

        public Protocol(string name, bool addAllProbes)
        {
            _name = name;
            _probes = new List<Probe>();
            _running = false;

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

        [OnDeserialized]
        private void PostDeserialization(StreamingContext c)
        {
            foreach (Probe probe in _probes)
                probe.Protocol = this;

            if (_localDataStore != null)
                _localDataStore.Protocol = this;

            if (_remoteDataStore != null)
                _remoteDataStore.Protocol = this;
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
                            catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to stop " + probe.Name + "'s controller:  " + ex.Message + Environment.NewLine + ex.StackTrace); }

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
    }
}

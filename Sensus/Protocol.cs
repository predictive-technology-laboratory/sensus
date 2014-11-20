using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sensus
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private LocalDataStore _localDataStore;
        private RemoteDataStore _remoteDataStore;
        private PropertyChangedEventHandler _notifyWatchersOfProbesChange;

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
                        App.Get().SensusService.StopProtocol(this);
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
                }
            }
        }

        public Protocol(string name, bool addAllProbes)
        {
            _name = name;
            _probes = new List<Probe>();
            _running = false;

            _notifyWatchersOfProbesChange = (o, e) =>
                {
                    OnPropertyChanged("Probes");
                };

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                    AddProbe(probe);
        }

        public void AddProbe(Probe probe)
        {
            probe.Protocol = this;
            probe.PropertyChanged += _notifyWatchersOfProbesChange;

            _probes.Add(probe);
        }

        public void RemoveProbe(Probe probe)
        {
            probe.Protocol = null;
            probe.PropertyChanged -= _notifyWatchersOfProbesChange;

            _probes.Remove(probe);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void StartAsync()
        {
            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Initializing and starting probes for protocol " + _name + ".");

            int probesStarted = 0;
            foreach (Probe probe in _probes)
                if (probe.Enabled && await probe.InitializeAndStartAsync())
                    probesStarted++;

            if (probesStarted > 0)
            {
                if (App.LoggingLevel >= LoggingLevel.Normal)
                    App.Get().SensusService.Log("Starting local data store.");

                try
                {
                    _localDataStore.Start(this);

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Local data store started.");
                }
                catch (Exception ex)
                {
                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Local data store failed to start:  " + ex.Message + Environment.NewLine + ex.StackTrace);

                    Running = false;
                    return;
                }

                if (App.LoggingLevel >= LoggingLevel.Normal)
                    App.Get().SensusService.Log("Starting remote data store.");

                try
                {
                    _remoteDataStore.Start(_localDataStore);

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Remote data store started.");
                }
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
        }

        public Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // if the service is stopping this protocol, then _running/Running will be true here. set it to false to release the probes and data stores below.
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

        public void ClearPropertyChangedDelegates()
        {
            PropertyChanged = null;

            foreach (Probe probe in _probes)
                probe.ClearPropertyChangedDelegates();

            if (_localDataStore != null)
                _localDataStore.ClearPropertyChangedDelegates();

            if (_remoteDataStore != null)
                _remoteDataStore.ClearPropertyChangedDelegates();
        }
    }
}

using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
                    {
                        if (Logger.Level >= LoggingLevel.Normal)
                            Logger.Log("Initializing and starting probes.");

                        int probesStarted = 0;
                        foreach (Probe probe in _probes)
                            if (probe.Initialize() == ProbeState.Initialized)
                            {
                                if (probe.Enabled)
                                {
                                    try
                                    {
                                        probe.StartAsync();
                                        if (probe.State == ProbeState.Started)
                                        {
                                            if (Logger.Level >= LoggingLevel.Normal)
                                                Logger.Log("Probe \"" + probe.Name + "\" started.");
                                        }
                                        else
                                            throw new Exception("Probe.Start method returned without error but the probe state is \"" + probe.State + "\".");
                                    }
                                    catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to start probe \"" + probe.Name + "\":" + ex.Message + Environment.NewLine + ex.StackTrace); }

                                    if (probe.State == ProbeState.Started)
                                        probesStarted++;
                                }
                            }

                        if (probesStarted > 0)
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("Starting local data store.");

                            try
                            {
                                _localDataStore.StartAsync(this);

                                if (Logger.Level >= LoggingLevel.Normal)
                                    Logger.Log("Local data store started.");
                            }
                            catch (Exception ex)
                            {
                                if (Logger.Level >= LoggingLevel.Normal)
                                    Logger.Log("Local data store failed to start:  " + ex.Message + Environment.NewLine + ex.StackTrace);

                                Running = false;
                                return;
                            }

                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("Starting remote data store.");

                            try
                            {
                                _remoteDataStore.StartAsync(_localDataStore);

                                if (Logger.Level >= LoggingLevel.Normal)
                                    Logger.Log("Remote data store started.");
                            }
                            catch (Exception ex)
                            {
                                if (Logger.Level >= LoggingLevel.Normal)
                                    Logger.Log("Remote data store failed to start:  " + ex.Message);

                                Running = false;
                                return;
                            }
                        }
                        else
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("No probes were started.");

                            Running = false;
                        }
                    }
                    else
                    {
                        if (Logger.Level >= LoggingLevel.Normal)
                            Logger.Log("Stopping probes.");

                        foreach (Probe probe in _probes)
                            if (probe.State == ProbeState.Started)
                                try { probe.StopAsync(); }
                                catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to stop probe:  " + ex.Message + Environment.NewLine + ex.StackTrace); }

                        if (_localDataStore.Running)
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("Stopping local data store.");

                            try { _localDataStore.StopAsync(); }
                            catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to stop local data store:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                        }

                        if (_remoteDataStore.Running)
                        {
                            if (Logger.Level >= LoggingLevel.Normal)
                                Logger.Log("Stopping remote data store.");

                            try { _remoteDataStore.StopAsync(); }
                            catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to stop remote data store:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                        }
                    }
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
    }
}

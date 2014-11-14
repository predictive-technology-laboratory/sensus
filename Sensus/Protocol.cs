using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Exceptions;
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
                if(!value.Equals(_name, StringComparison.Ordinal))
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
                        Console.Error.WriteLine("Testing local data store.");
                        try { _localDataStore.Test(); }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Local data store test failed:  " + ex.Message);
                            Running = false;
                            return;
                        }

                        Console.Error.WriteLine("Testing remote data store.");
                        try { _remoteDataStore.Test(); }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Remote data store test failed:  " + ex.Message);
                            Running = false;
                            return;
                        }

                        Console.Error.WriteLine("Initializing and starting probes.");
                        App.Get().ProbeInitializer.Initialize(_probes);
                        int probesStarted = 0;
                        foreach (Probe probe in _probes)
                            if (probe.State == ProbeState.Initialized)
                            {
                                try
                                {
                                    probe.Start();
                                    if (probe.State == ProbeState.Started)
                                        Console.Error.WriteLine("Probe \"" + probe.Name + "\" started.");
                                    else
                                        throw new Exception("Probe.Start method returned without error but the probe state is \"" + probe.State + "\".");
                                }
                                catch (Exception ex) { Console.Error.WriteLine("Failed to start probe \"" + probe.Name + "\":" + ex.Message); }

                                if (probe.State == ProbeState.Started)
                                    probesStarted++;
                            }

                        if (probesStarted > 0)
                        {
                            Console.Error.WriteLine("Starting local data store.");
                            try
                            {
                                _localDataStore.Start(this);
                                Console.Error.WriteLine("Local data store started.");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Local data store failed to start:  " + ex.Message);
                                Running = false;
                                return;
                            }

                            Console.Error.WriteLine("Starting remote data store.");
                            try
                            {
                                _remoteDataStore.Start(_localDataStore);
                                Console.Error.WriteLine("Remote data store started.");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Remote data store failed to start:  " + ex.Message);
                                Running = false;
                                return;
                            }
                        }
                        else
                            Running = false;
                    }
                    else
                    {
                        Console.Error.WriteLine("Stopping probes.");
                        foreach (Probe probe in _probes)
                            if (probe.State == ProbeState.Started)
                                try { probe.Stop(); }
                                catch (Exception ex) { Console.Error.WriteLine("Failed to stop probe:  " + ex.Message); }

                        if (_localDataStore.Running)
                        {
                            Console.Error.WriteLine("Stopping local data store.");
                            try { _localDataStore.Stop(); }
                            catch (Exception ex) { Console.Error.WriteLine("Failed to stop local data store:  " + ex.Message); }
                        }

                        if (_remoteDataStore.Running)
                        {
                            Console.Error.WriteLine("Stopping remote data store.");
                            try { _remoteDataStore.Stop(); }
                            catch (Exception ex) { Console.Error.WriteLine("Failed to stop remote data store:  " + ex.Message); }
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
            probe.PropertyChanged += _notifyWatchersOfProbesChange;
            _probes.Add(probe);
        }

        public void RemoveProbe(Probe probe)
        {
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

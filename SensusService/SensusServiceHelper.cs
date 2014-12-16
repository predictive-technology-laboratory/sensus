using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusService.Probes;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Xamarin.Geolocation;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper : INotifyPropertyChanged
    {
        #region static members
        private static SensusServiceHelper _singleton;
        private static object _staticLockObject = new object();

        public static SensusServiceHelper Get()
        {
            // service helper be null for a brief period between the time when the app starts and when the service constructs the helper object.
            int triesLeft = 10;
            while (triesLeft-- > 0)
            {
                lock (_staticLockObject)
                    if (_singleton == null)
                    {
                        Console.Error.WriteLine("Waiting for service to construct helper object.");
                        Thread.Sleep(1000);
                    }
                    else
                        break;
            }

            lock (_staticLockObject)
                if (_singleton == null)
                {
                    string error = "Failed to get service helper.";
                    Console.Error.WriteLine(error);
                    throw new Exception(error);
                }

            return _singleton;
        }
        #endregion

        /// <summary>
        /// Raised when a UI-relevant property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when the service helper has stopped.
        /// </summary>
        public event EventHandler Stopped;

        private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private readonly string _logTag = "SERVICE-HELPER";
        private readonly string _savedProtocolsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "saved_protocols.json");
        private readonly string _runningProtocolIdsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "running_protocol_ids.json");
        private readonly JsonSerializerSettings _protocolSerializationSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        private bool _stopped;
        private Logger _logger;
        private List<Protocol> _registeredProtocols;

        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> RegisteredProtocols
        {
            get { return _registeredProtocols; }
        }

        [DisplayYesNoUiProperty("Charging:", 1)]
        public abstract bool IsCharging { get; }

        [DisplayYesNoUiProperty("WiFi Connected:", 2)]
        public abstract bool WiFiConnected { get; }

        [DisplayStringUiProperty("Device ID:", int.MaxValue)]
        public abstract string DeviceId { get; }

        protected SensusServiceHelper(Geolocator geolocator)
        {
            GpsReceiver.Get().Initialize(geolocator);

            _stopped = true;

            #region logger
#if DEBUG
            _logger = new Logger(_logPath, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, LoggingLevel.Normal, Console.Error);
#endif

            _logger.Log("Log file started at \"" + _logPath + "\".", LoggingLevel.Normal, _logTag);
            #endregion

            _registeredProtocols = ReadSavedProtocols();

            _logger.Log("Loaded " + _registeredProtocols.Count + " protocols.", LoggingLevel.Normal, _logTag);

            lock (_staticLockObject)
                _singleton = this;
        }

        #region save/read protocols
        private void SaveRegisteredProtocols()
        {
            lock (this)
            {
                try
                {
                    using (StreamWriter file = new StreamWriter(_savedProtocolsPath))
                    {
                        file.Write(JsonConvert.SerializeObject(_registeredProtocols, Formatting.Indented, _protocolSerializationSettings));
                        file.Close();
                    }
                }
                catch (Exception ex) { _logger.Log("Failed to save protocols:  " + ex.Message, LoggingLevel.Normal, _logTag); }
            }
        }

        private List<Protocol> ReadSavedProtocols()
        {
            lock (this)
            {
                List<Protocol> protocols = null;

                if (File.Exists(_savedProtocolsPath))
                    try
                    {
                        using (StreamReader file = new StreamReader(_savedProtocolsPath))
                        {
                            protocols = JsonConvert.DeserializeObject<List<Protocol>>(file.ReadToEnd(), _protocolSerializationSettings);
                            file.Close();
                        }
                    }
                    catch (Exception ex) { _logger.Log("Failed to read protocols from existing path \"" + _savedProtocolsPath + "\":  " + ex.Message, LoggingLevel.Normal, _logTag); }
                else
                    _logger.Log("No saved protocols file exists.", LoggingLevel.Normal, _logTag);

                if (protocols == null)
                    protocols = new List<Protocol>();

                return protocols;
            }
        }
        #endregion

        #region running protocol ids
        private void AddRunningProtocolId(string id)
        {
            lock (this)
            {
                List<string> ids = ReadRunningProtocolIds();
                if (ids.Contains(id))
                    return;
                else
                {
                    ids.Add(id);
                    SaveRunningProtocolIds(ids);
                }
            }
        }

        private void RemoveRunningProtocolId(string id)
        {
            lock (this)
            {
                List<string> ids = ReadRunningProtocolIds();
                if (ids.Remove(id))
                    SaveRunningProtocolIds(ids);
            }
        }

        private void SaveRunningProtocolIds(List<string> ids)
        {
            lock (this)
                try
                {
                    using (StreamWriter file = new StreamWriter(_runningProtocolIdsPath))
                    {
                        file.Write(JsonConvert.SerializeObject(ids, Formatting.Indented));
                        file.Close();
                    }
                }
                catch (Exception ex) { _logger.Log("Failed to save running protocol ID list:  " + ex.Message, LoggingLevel.Normal, _logTag); }
        }

        private List<string> ReadRunningProtocolIds()
        {
            lock (this)
            {
                List<string> ids = null;
                try
                {
                    using (StreamReader file = new StreamReader(_runningProtocolIdsPath))
                    {
                        ids = JsonConvert.DeserializeObject<List<string>>(file.ReadToEnd());
                        file.Close();
                    }
                }
                catch (Exception ex) { _logger.Log("Failed to read running protocol ID list:  " + ex.Message, LoggingLevel.Normal); }

                if (ids == null)
                    ids = new List<string>();

                return ids;
            }
        }
        #endregion

        /// <summary>
        /// Starts platform-independent service functionality. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                if (_stopped)
                    _stopped = false;
                else
                    return;

                List<string> runningProtocolIds = ReadRunningProtocolIds();
                foreach (Protocol protocol in _registeredProtocols)
                    if (!protocol.Running && runningProtocolIds.Contains(protocol.Id))
                        StartProtocolAsync(protocol);
            }
        }

        /// <summary>
        /// Stops the service helper, but leaves it in a state in which subsequent calls to Start will succeed. This happens, for example, when the service is stopped and then 
        /// restarted without being destroyed.
        /// </summary>
        public Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // prevent any future interactions with the SensusServiceHelper
                    lock (this)
                        if (_stopped)
                            return;
                        else
                            _stopped = true;

                    _logger.Log("Stopping Sensus service.", LoggingLevel.Normal, _logTag);

                    foreach (Protocol protocol in _registeredProtocols)
                        await StopProtocolAsync(protocol, false);

                    // let others (e.g., platform-specific services and applications) know that we've stopped
                    if (Stopped != null)
                        Stopped(null, null);
                });
        }

        public void RegisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    if (!_registeredProtocols.Contains(protocol))
                    {
                        _registeredProtocols.Add(protocol);
                        SaveRegisteredProtocols();
                    }
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    if (_registeredProtocols.Remove(protocol))
                        SaveRegisteredProtocols();
        }

        protected abstract void StartSensusMonitoring();

        protected abstract void StopSensusMonitoring();

        public Task StartProtocolAsync(Protocol protocol)
        {
            return Task.Run(() =>
                {
                    lock (this)
                        lock (protocol)
                        {
                            if (_stopped || protocol.Running)
                                return;

                            protocol.SetRunning(true);

                            RegisterProtocol(protocol);
                            AddRunningProtocolId(protocol.Id);
                            StartSensusMonitoring();

                            _logger.Log("Initializing and starting probes for protocol " + protocol.Name + ".", LoggingLevel.Normal);
                            int probesStarted = 0;
                            foreach (Probe probe in protocol.Probes)
                                if (probe.Enabled && probe.InitializeAndStart())
                                    probesStarted++;

                            bool stopProtocol = false;

                            if (probesStarted > 0)
                            {
                                try
                                {
                                    protocol.LocalDataStore.Start();

                                    try { protocol.RemoteDataStore.Start(); }
                                    catch (Exception ex)
                                    {
                                        _logger.Log("Remote data store failed to start:  " + ex.Message, LoggingLevel.Normal);
                                        stopProtocol = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Log("Local data store failed to start:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal);
                                    stopProtocol = true;
                                }
                            }
                            else
                            {
                                _logger.Log("No probes were started.", LoggingLevel.Normal);
                                stopProtocol = true;
                            }

                            if (stopProtocol)
                                StopProtocolAsync(protocol, false);
                        }
                });
        }

        public Task StopProtocolAsync(Protocol protocol, bool unregister)
        {
            return Task.Run(() =>
                {
                    lock (this)
                        lock (protocol)
                        {
                            if (_stopped || !protocol.Running)
                                return;

                            protocol.SetRunning(false);

                            if (unregister)
                                UnregisterProtocol(protocol);

                            RemoveRunningProtocolId(protocol.Id);

                            if (_registeredProtocols.Count(p => p.Running) == 0)
                                StopSensusMonitoring();

                            _logger.Log("Stopping probes.", LoggingLevel.Normal);
                            foreach (Probe probe in protocol.Probes)
                                if (probe.Controller.Running)
                                    try { probe.Controller.Stop(); }
                                    catch (Exception ex) { _logger.Log("Failed to stop " + probe.DisplayName + "'s controller:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }

                            if (protocol.LocalDataStore != null && protocol.LocalDataStore.Running)
                            {
                                _logger.Log("Stopping local data store.", LoggingLevel.Normal);

                                try { protocol.LocalDataStore.Stop(); }
                                catch (Exception ex) { _logger.Log("Failed to stop local data store:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }
                            }

                            if (protocol.RemoteDataStore != null && protocol.RemoteDataStore.Running)
                            {
                                _logger.Log("Stopping remote data store.", LoggingLevel.Normal);

                                try { protocol.RemoteDataStore.Stop(); }
                                catch (Exception ex) { _logger.Log("Failed to stop remote data store:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }
                            }
                        }
                });
        }

        public async void Ping()
        {
            lock (this)
                if (_stopped)
                    return;

            List<string> runningProtocolIds = ReadRunningProtocolIds();
            foreach (Protocol protocol in _registeredProtocols)
            {
                if (!protocol.Running && runningProtocolIds.Contains(protocol.Id))
                    await StartProtocolAsync(protocol);

                // TODO:  Check datastores

                // TODO:  Check probes
            }
        }

        public void Destroy()
        {
            _logger.Log("Destroying Sensus service helper.", LoggingLevel.Normal, _logTag);

            StopAsync().Wait();

            _registeredProtocols = null;
            _logger.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract void ShareFile(string path, string emailSubject);

        public string GetTempPath(string extension)
        {
            string path = null;
            while (path == null)
            {
                string tempPath = Path.GetTempFileName();
                File.Delete(tempPath);

                if (!string.IsNullOrWhiteSpace(extension))
                    tempPath = Path.Combine(Path.GetDirectoryName(tempPath), Path.GetFileNameWithoutExtension(tempPath) + "." + extension.Trim('.'));

                if (!File.Exists(tempPath))
                    path = tempPath;
            }

            return path;
        }
    }
}

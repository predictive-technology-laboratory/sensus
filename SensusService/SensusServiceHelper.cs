using Newtonsoft.Json;
using SensusService.Probes;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin;
using Xamarin.Geolocation;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper : INotifyPropertyChanged
    {
        #region singleton management
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

                    // don't try to access/write the logger or raise a sensus-based exception, since these will call back into the current method
                    Console.Error.WriteLine(error);

                    Exception ex = new Exception(error);

                    try { Insights.Report(ex, ReportSeverity.Error); }
                    catch (Exception ex2) { Console.Error.WriteLine("Failed to report exception to Xamarin Insights:  " + ex2.Message); }

                    throw ex;
                }

            return _singleton;
        }
        #endregion

        protected const string XAMARIN_INSIGHTS_APP_KEY = "97af5c4ab05c6a69d2945fd403ff45535f8bb9bb";

        /// <summary>
        /// Raised when a UI-relevant property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when the service helper has stopped.
        /// </summary>
        public event EventHandler Stopped;

        private readonly string _shareDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
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
        private int _pingDelayMS;
        private int _pingCount;
        private int _pingsPerProtocolReportUpload;

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

        [EntryIntegerUiProperty("Ping Delay (MS):", true, int.MaxValue)]
        public int PingDelayMS
        {
            get { return _pingDelayMS; }
            set
            {
                if (value != _pingDelayMS)
                {
                    _pingDelayMS = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Pings Per Report Upload:", true, int.MaxValue)]
        public int PingsPerProtocolReportUpload
        {
            get { return _pingsPerProtocolReportUpload; }
            set
            {
                if (value != _pingsPerProtocolReportUpload)
                {
                    _pingsPerProtocolReportUpload = value;
                    OnPropertyChanged();
                }
            }
        }

        protected SensusServiceHelper(Geolocator geolocator)
        {
            GpsReceiver.Get().Initialize(geolocator);

            _stopped = true;
            _pingDelayMS = 1000 * 60;
            _pingCount = 0;
            _pingsPerProtocolReportUpload = 5;

            if (!Directory.Exists(_shareDirectory))
                Directory.CreateDirectory(_shareDirectory);

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

            try { InitializeXamarinInsights(); }
            catch (Exception ex) { _logger.Log("Failed to initialize Xamarin insights:  " + ex.Message, LoggingLevel.Normal); }

            lock (_staticLockObject)
                _singleton = this;
        }

        protected abstract void InitializeXamarinInsights();

        #region save/read protocols
        public void SaveRegisteredProtocols()
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
                    _logger.Log("No saved protocols file exists at \"" + _savedProtocolsPath + "\".", LoggingLevel.Normal, _logTag);

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

        public bool ProtocolShouldBeRunning(Protocol protocol)
        {
            return ReadRunningProtocolIds().Contains(protocol.Id);
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

        public Task StartProtocolAsync(Protocol protocol)
        {
            return Task.Run(() => StartProtocol(protocol));
        }

        public void StartProtocol(Protocol protocol)
        {
            lock (this)
                lock (protocol)
                {
                    if (_stopped || protocol.Running)
                        return;

                    protocol.SetRunning(true);

                    RegisterProtocol(protocol);
                    AddRunningProtocolId(protocol.Id);
                    StartSensusPings(_pingDelayMS);

                    _logger.Log("Starting probes for protocol " + protocol.Name + ".", LoggingLevel.Normal);
                    int probesStarted = 0;
                    foreach (Probe probe in protocol.Probes)
                        if (probe.Enabled)
                            try
                            {
                                probe.Start();
                                probesStarted++;
                            }
                            catch (Exception ex) { _logger.Log("Failed to start probe \"" + probe.GetType().FullName + "\":" + ex.Message, LoggingLevel.Normal); }

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
                            _logger.Log("Local data store failed to start:  " + ex.Message, LoggingLevel.Normal);
                            stopProtocol = true;
                        }
                    }
                    else
                    {
                        _logger.Log("No probes were started.", LoggingLevel.Normal);
                        stopProtocol = true;
                    }

                    if (stopProtocol)
                        StopProtocol(protocol, false);
                }
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

        public Task StopProtocolAsync(Protocol protocol, bool unregister)
        {
            return Task.Run(() => StopProtocol(protocol, unregister));
        }

        public void StopProtocol(Protocol protocol, bool unregister)
        {
            lock (this)
                lock (protocol)
                {
                    if (_stopped)
                        return;

                    if (unregister)
                        UnregisterProtocol(protocol);

                    if (!protocol.Running)
                        return;

                    protocol.SetRunning(false);

                    RemoveRunningProtocolId(protocol.Id);

                    if (_registeredProtocols.Count(p => p.Running) == 0)
                        StopSensusPings();

                    _logger.Log("Stopping probes.", LoggingLevel.Normal);
                    foreach (Probe probe in protocol.Probes)
                        if (probe.Running)
                            try { probe.Stop(); }
                            catch (Exception ex) { _logger.Log("Failed to stop " + probe.GetType().FullName + ":  " + ex.Message, LoggingLevel.Normal); }

                    if (protocol.LocalDataStore != null && protocol.LocalDataStore.Running)
                    {
                        _logger.Log("Stopping local data store.", LoggingLevel.Normal);

                        try { protocol.LocalDataStore.Stop(); }
                        catch (Exception ex) { _logger.Log("Failed to stop local data store:  " + ex.Message, LoggingLevel.Normal); }
                    }

                    if (protocol.RemoteDataStore != null && protocol.RemoteDataStore.Running)
                    {
                        _logger.Log("Stopping remote data store.", LoggingLevel.Normal);

                        try { protocol.RemoteDataStore.Stop(); }
                        catch (Exception ex) { _logger.Log("Failed to stop remote data store:  " + ex.Message, LoggingLevel.Normal); }
                    }
                }
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    if (_registeredProtocols.Remove(protocol))
                        SaveRegisteredProtocols();
        }

        /// <summary>
        /// Stops the service helper, but leaves it in a state in which subsequent calls to Start will succeed. This happens, for example, when the service is stopped and then 
        /// restarted without being destroyed.
        /// </summary>
        public Task StopAsync()
        {
            return Task.Run(() =>
                {
                    lock (this)
                    {
                        if (_stopped)
                            return;

                        _logger.Log("Stopping Sensus service.", LoggingLevel.Normal, _logTag);

                        foreach (Protocol protocol in _registeredProtocols)
                            StopProtocol(protocol, false);

                        _stopped = true;
                    }

                    // let others (e.g., platform-specific services and applications) know that we've stopped
                    if (Stopped != null)
                        Stopped(null, null);
                });
        }

        protected abstract void StartSensusPings(int ms);

        protected abstract void StopSensusPings();

        public void Ping()
        {
            lock (this)
            {
                if (_stopped)
                    return;

                _logger.Log("Sensus service helper was pinged (count=" + ++_pingCount + ")", LoggingLevel.Normal, _logTag);

                List<string> runningProtocolIds = ReadRunningProtocolIds();
                foreach (Protocol protocol in _registeredProtocols)
                    if (runningProtocolIds.Contains(protocol.Id))
                    {
                        protocol.Ping();

                        if (_pingCount % _pingsPerProtocolReportUpload == 0)
                            protocol.UploadMostRecentProtocolReport();
                    }
            }
        }

        public void Destroy()
        {
            try { _logger.Close(); }
            catch (Exception) { }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract void ShareFile(string path, string subject);

        public string GetSharePath(string extension)
        {
            lock (this)
            {
                int fileNum = 0;
                string path = null;
                while (path == null || File.Exists(path))
                    path = Path.Combine(_shareDirectory, fileNum++ + (string.IsNullOrWhiteSpace(extension) ? "" : "." + extension.Trim('.')));

                return path;
            }
        }
    }
}

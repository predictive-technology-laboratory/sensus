using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        private static string _protocolsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "protocols.json");
        private static string _previouslyRunningProtocolsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "previously_running_protocols.json");

        private static string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
        private static string _logTag = "SERVICE-HELPER";

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
                        Console.Error.WriteLine(_logTag + ":  Waiting for service to construct helper object.");
                        Thread.Sleep(1000);
                    }
                    else
                        break;
            }

            lock (_staticLockObject)
                if (_singleton == null)
                {
                    string error = _logTag + ":  Failed to get service helper.";
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

        private bool _stopped;
        private Logger _logger;
        private List<Protocol> _registeredProtocols;
        private bool _autoRestart;

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

        [OnOffUiProperty("Auto-Restart:", true, 0)]
        public bool AutoRestart
        {
            get { return _autoRestart; }
            set
            {
                if (value != _autoRestart)
                {
                    _autoRestart = value;
                    SetAutoRestart(_autoRestart);
                    OnPropertyChanged();
                }
            }
        }

        

        protected SensusServiceHelper(Geolocator geolocator, bool autoRestart)
        {
            GpsReceiver.Get().Initialize(geolocator);
            AutoRestart = autoRestart;
            _stopped = true;

            #region logger
#if DEBUG
            _logger = new Logger(_logPath, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, LoggingLevel.Normal, Console.Error);
#endif

            _logger.Log("Log file started at \"" + _logPath + "\".", LoggingLevel.Normal, _logTag);
            #endregion

            #region get saved protocols
            _registeredProtocols = new List<Protocol>();

            if (File.Exists(_protocolsPath))
                try
                {
                    using (StreamReader protocolsFile = new StreamReader(_protocolsPath))
                    {
                        _registeredProtocols = JsonConvert.DeserializeObject<List<Protocol>>(protocolsFile.ReadToEnd(), new JsonSerializerSettings
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                            TypeNameHandling = TypeNameHandling.All,
                            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                        });

                        protocolsFile.Close();
                    }
                }
                catch (Exception ex) { _logger.Log("Failed to read serialized protocols from existing path \"" + _protocolsPath + "\":  " + ex.Message, LoggingLevel.Normal, _logTag); }
            else
                _logger.Log("No protocols serialization file exists. Proceeding with no saved protocols.", LoggingLevel.Normal, _logTag);

            _logger.Log("Deserialized " + _registeredProtocols.Count + " protocols.", LoggingLevel.Normal, _logTag);
            #endregion

            lock (_staticLockObject)
                _singleton = this;
        }

        protected abstract void SetAutoRestart(bool enabled);

        /// <summary>
        /// Starts platform-independent service functionality. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void Start()
        {
            lock (this)
                if (_stopped)
                    _stopped = false;
                else
                    return;

            try
            {
                List<string> previouslyRunningProtocols = new List<string>();

                using (StreamReader previouslyRunningProtocolsFile = new StreamReader(_previouslyRunningProtocolsPath))
                {
                    previouslyRunningProtocols = JsonConvert.DeserializeObject<List<string>>(previouslyRunningProtocolsFile.ReadToEnd());
                    previouslyRunningProtocolsFile.Close();
                }

                foreach (Protocol protocol in _registeredProtocols)
                    if (!protocol.Running && previouslyRunningProtocols.Contains(protocol.Id))
                    {
                        _logger.Log("Starting previously running protocol:  " + protocol.Name, LoggingLevel.Normal, _logTag);

                        StartProtocolAsync(protocol);
                    }
            }
            catch (Exception ex) { _logger.Log("Failed to deserialize IDs for previously running protocols:  " + ex.Message, LoggingLevel.Normal, _logTag); }
        }

        public void RegisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    if (!_registeredProtocols.Contains(protocol))
                        _registeredProtocols.Add(protocol);
        }

        public Task StartProtocolAsync(Protocol protocol)
        {
            lock (this)
                if (_stopped)
                    return null;
                else
                {
                    if (!_registeredProtocols.Contains(protocol))  // can't call RegisterProtocol here due to locking -- just repeat the code
                        _registeredProtocols.Add(protocol);

                    return protocol.StartAsync();
                }
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

        public Task StopProtocolAsync(Protocol protocol, bool unregister)
        {
            lock (this)
                if (_stopped)
                    return null;
                else
                {
                    if (unregister)
                        _registeredProtocols.Remove(protocol);

                    return protocol.StopAsync();
                }
        }

        /// <summary>
        /// Stops the service helper, but leaves it in a state in which subsequent calls to Start will succeed. This happens, for example, when the service is stopped and then 
        /// restarted without being destroyed.
        /// </summary>
        /// <returns></returns>
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

                    List<string> runningProtocolIds = new List<string>();

                    foreach (Protocol protocol in _registeredProtocols)
                        if (protocol.Running)
                        {
                            runningProtocolIds.Add(protocol.Id);
                            await protocol.StopAsync();
                        }

                    try
                    {
                        using (StreamWriter protocolsFile = new StreamWriter(_protocolsPath))
                        {
                            protocolsFile.Write(JsonConvert.SerializeObject(_registeredProtocols, Formatting.Indented, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                                TypeNameHandling = TypeNameHandling.All,
                                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                            }));

                            protocolsFile.Close();
                        }
                    }
                    catch (Exception ex) { _logger.Log("Failed to serialize protocols:  " + ex.Message, LoggingLevel.Normal, _logTag); }

                    try
                    {
                        using (StreamWriter previouslyRunningProtocolsFile = new StreamWriter(_previouslyRunningProtocolsPath))
                        {
                            previouslyRunningProtocolsFile.Write(JsonConvert.SerializeObject(runningProtocolIds, Formatting.Indented));
                            previouslyRunningProtocolsFile.Close();
                        }
                    }
                    catch (Exception ex) { _logger.Log("Failed to serialize running protocol ID list:  " + ex.Message, LoggingLevel.Normal, _logTag); }

                    // let others (e.g., platform-specific services and applications) know that we've stopped
                    if (Stopped != null)
                        Stopped(null, null);
                });
        }

        public async void Destroy()
        {
            _logger.Log("Destroying Sensus service helper.", LoggingLevel.Normal, _logTag);

            await StopAsync();

            _registeredProtocols = null;

            _logger.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

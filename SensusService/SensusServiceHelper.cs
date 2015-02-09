#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Newtonsoft.Json;
using SensusService.Probes.Location;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xamarin;
using Xamarin.Geolocation;

namespace SensusService
{
    /// <summary>
    /// Provides platform-independent service functionality.
    /// </summary>
    public abstract class SensusServiceHelper
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
        private int _pingsPerProtocolReport;

        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> RegisteredProtocols
        {
            get { return _registeredProtocols; }
        }

        public abstract bool IsCharging { get; }

        public abstract bool WiFiConnected { get; }

        public abstract string DeviceId { get; }

        public abstract bool DeviceHasMicrophone { get; }

        [EntryIntegerUiProperty("Ping Delay (MS):", true, 9)]
        public int PingDelayMS
        {
            get { return _pingDelayMS; }
            set { _pingDelayMS = value; }
        }

        [EntryIntegerUiProperty("Pings Per Report:", true, 10)]
        public int PingsPerProtocolReport
        {
            get { return _pingsPerProtocolReport; }
            set { _pingsPerProtocolReport = value; }
        }

        [ListUiProperty("Logging Level:", true, 11, new object[] { LoggingLevel.Off, LoggingLevel.Normal, LoggingLevel.Verbose, LoggingLevel.Debug })]
        public LoggingLevel LoggingLevel
        {
            get { return _logger.Level; }
            set { _logger.Level = value; }
        }

        protected SensusServiceHelper(Geolocator geolocator)
        {
            GpsReceiver.Get().Initialize(geolocator);

            _stopped = true;
            _pingDelayMS = 1000 * 60;
            _pingCount = 0;
            _pingsPerProtocolReport = 5;

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

        #region platform-specific abstract methods
        protected abstract void InitializeXamarinInsights();

        public abstract void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback);

        public abstract void ShareFileAsync(string path, string subject);

        protected abstract void StartSensusPings(int ms);

        protected abstract void StopSensusPings();

        public void TextToSpeechAsync(string text)
        {
            TextToSpeechAsync(text, () =>
                {
                });                        
        }

        public abstract void TextToSpeechAsync(string text, Action callback);

        public abstract void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback);

        public void FlashNotificationAsync(string message)
        {
            FlashNotificationAsync(message, () =>
                {
                });
        }

        public abstract void FlashNotificationAsync(string message, Action callback);
        #endregion

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
        public void AddRunningProtocolId(string id)
        {
            lock (this)
            {
                List<string> ids = ReadRunningProtocolIds();
                if (!ids.Contains(id))
                {
                    ids.Add(id);
                    SaveRunningProtocolIds(ids);
                }

                StartSensusPings(_pingDelayMS);
            }
        }

        public void RemoveRunningProtocolId(string id)
        {
            lock (this)
            {
                List<string> ids = ReadRunningProtocolIds();
                if (ids.Remove(id))
                    SaveRunningProtocolIds(ids);

                if (ids.Count == 0)
                    StopSensusPings();
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
        /// Starts platform-independent service functionality, including protocols that should be running. Okay to call multiple times, even if the service is already running.
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
                        protocol.Start();
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

        public void PingAsync()
        {
            new Thread(() =>
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

                                if (_pingCount % _pingsPerProtocolReport == 0)
                                    protocol.StoreMostRecentProtocolReport();
                            }
                    }
                }).Start();
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
        public void StopAsync()
        {
            new Thread(() =>
                {
                    lock (this)
                    {
                        if (_stopped)
                            return;

                        _logger.Log("Stopping Sensus service.", LoggingLevel.Normal, _logTag);

                        foreach (Protocol protocol in _registeredProtocols)
                            protocol.Stop();

                        _stopped = true;
                    }

                    // let others (e.g., platform-specific services and applications) know that we've stopped
                    if (Stopped != null)
                        Stopped(null, null);

                }).Start();
        }

        public virtual void Destroy()
        {
            try { _logger.Close(); }
            catch (Exception) { }
        }

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

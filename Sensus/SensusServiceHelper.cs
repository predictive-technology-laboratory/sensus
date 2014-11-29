using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Sensus.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Sensus
{
    public class SensusServiceHelper
    {
        private static string _protocolsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "protocols.json");
        private static string _previouslyRunningProtocolsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "previously_running_protocols.json");

        private List<Protocol> _registeredProtocols;
        private Logger _logger;
        private readonly string _logPath;
        private bool _stopped;

        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> RegisteredProtocols
        {
            get
            {
                // registered protocols get deserialized on service startup. wait for them here.
                int triesLeft = 5;
                while (triesLeft-- > 0)
                {
                    lock (this)
                        if (_registeredProtocols == null)
                        {
                            if (_logger.Level >= LoggingLevel.Normal)
                                _logger.WriteLine("Waiting for registered protocols to be deserialized.");

                            Thread.Sleep(1000);
                        }
                        else
                            return _registeredProtocols;
                }

                throw new SensusException("Failed to get registered protocols.");
            }
        }

        public SensusServiceHelper()
        {
            _stopped = true;
            _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");

#if DEBUG
            _logger = new Logger(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif

            if (_logger.Level >= LoggingLevel.Normal)
                _logger.WriteLine("Log file started at \"" + _logPath + "\".");
        }

        /// <summary>
        /// Starts platform-independent service functionality. Okay to call multiple times, even if the service is already running.
        /// </summary>
        public void StartService()
        {
            lock (this)
                if (_stopped)
                    _stopped = false;
                else
                    return;

            _registeredProtocols = new List<Protocol>();

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
            catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to deserialize protocols:  " + ex.Message); }

            if (_logger.Level >= LoggingLevel.Normal)
                _logger.WriteLine("Deserialized " + _registeredProtocols.Count + " protocols.");

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
                        if (_logger.Level >= LoggingLevel.Normal)
                            _logger.WriteLine("Starting previously running protocol:  " + protocol.Name);

                        StartProtocolAsync(protocol);
                    }
            }
            catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to deserialize ids for previously running protocols:  " + ex.Message); }
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

        public Task StopProtocolAsync(Protocol protocol, bool unregister)
        {
            lock (this)
                if (_stopped)
                    return null;
                else
                {
                    Task stopTask = protocol.StopAsync();

                    if (unregister)
                        _registeredProtocols.Remove(protocol);

                    return stopTask;
                }
        }

        public async void StopServiceAsync()
        {
            // prevent any future interactions with the ServiceHelper
            lock (this)
                if (_stopped)
                    return;
                else
                    _stopped = true;

            if (_logger.Level >= LoggingLevel.Normal)
                _logger.WriteLine("Stopping Sensus service.");

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
            catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to serialize protocols:  " + ex.Message); }

            try
            {
                using (StreamWriter previouslyRunningProtocolsFile = new StreamWriter(_previouslyRunningProtocolsPath))
                {
                    previouslyRunningProtocolsFile.Write(JsonConvert.SerializeObject(runningProtocolIds, Formatting.Indented));
                    previouslyRunningProtocolsFile.Close();
                }
            }
            catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to serialize running protocol id list:  " + ex.Message); }
        }

        public void DestroyService()
        {
            _registeredProtocols = null;

            _logger.Close();
            _logger = null;
        }
    }
}

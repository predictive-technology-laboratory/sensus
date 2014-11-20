using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using Sensus.Exceptions;

namespace Sensus
{
    public class SensusServiceHelper
    {
        private static string _protocolsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_protocols.bin");

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
            _stopped = false;
            _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");

#if DEBUG
            _logger = new Logger(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif
        }

        public Task StartServiceAsync()
        {
            return Task.Run(() =>
                {
                    lock (this)
                    {
                        if (File.Exists(_protocolsPath))
                            try
                            {
                                BinaryFormatter bf = new BinaryFormatter();
                                FileStream protocolsFile = new FileStream(_protocolsPath, FileMode.Open, FileAccess.Read);
                                _registeredProtocols = bf.Deserialize(protocolsFile) as List<Protocol>;
                                protocolsFile.Close();

                                foreach (Protocol protocol in _registeredProtocols)
                                    protocol.DeserializationRebind();
                            }
                            catch (Exception ex) { if (App.LoggingLevel > LoggingLevel.Normal) App.Get().SensusService.Log("Failed to deserialize and/or rebind protocols:  " + ex.Message); }

                        if (_registeredProtocols == null)
                            _registeredProtocols = new List<Protocol>();
                    }
                });
        }

        public void StartProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                {
                    if (!_registeredProtocols.Contains(protocol))
                        _registeredProtocols.Add(protocol);

                    protocol.StartAsync();
                }
        }

        public void StopProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    protocol.StopAsync();
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    _registeredProtocols.Remove(protocol);
        }

        /// <summary>
        /// Stops all platform-independent service functionality. This include the logger, so no logging can be done after this method is called.
        /// </summary>
        /// <returns></returns>
        public Task StopServiceAsync()
        {
            // prevent any future interactions with the ServiceHelper
            lock (this)
                if (_stopped)
                    return null;
                else
                    _stopped = true;

            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Stopping Sensus service.");

            return Task.Run(async () =>
                {
                    foreach (Protocol protocol in _registeredProtocols)
                        if (protocol.Running)
                            await protocol.StopAsync();

                    try
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        FileStream protocolsFile = new FileStream(_protocolsPath, FileMode.Create, FileAccess.Write);
                        bf.Serialize(protocolsFile, _registeredProtocols);
                        protocolsFile.Close();
                    }
                    catch (Exception ex) { if (App.LoggingLevel > LoggingLevel.Normal) App.Get().SensusService.Log("Failed to serialize protocols:  " + ex.Message); }

                    _logger.Close();
                });
        }
    }
}

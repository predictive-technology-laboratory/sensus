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

        public void StartService()
        {
            lock (this)
                if (_stopped)
                    _stopped = false;
                else
                    return;

            if (File.Exists(_protocolsPath))
            {
                try
                {
                    using (FileStream protocolsFile = new FileStream(_protocolsPath, FileMode.Open, FileAccess.Read))
                        _registeredProtocols = new BinaryFormatter().Deserialize(protocolsFile) as List<Protocol>;
                }
                catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to deserialize protocols:  " + ex.Message); }
            }
            else if (_logger.Level >= LoggingLevel.Normal)
                _logger.WriteLine("No protocols file found at \"" + _protocolsPath + "\".");

            if (_registeredProtocols == null)
                _registeredProtocols = new List<Protocol>();
        }

        public void RegisterProtocol(Protocol protocol)
        {
            lock (this)
                if (!_stopped)
                    if (!_registeredProtocols.Contains(protocol))
                        _registeredProtocols.Add(protocol);
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

        public void StopProtocol(Protocol protocol, bool unregister)
        {
            lock (this)
                if (!_stopped)
                {
                    protocol.StopAsync();

                    if (unregister)
                        _registeredProtocols.Remove(protocol);
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

            foreach (Protocol protocol in _registeredProtocols)
                if (protocol.Running)
                    await protocol.StopAsync();

            try
            {
                using (FileStream protocolsFile = new FileStream(_protocolsPath, FileMode.Create, FileAccess.Write))
                    new BinaryFormatter().Serialize(protocolsFile, _registeredProtocols);
            }
            catch (Exception ex) { if (_logger.Level >= LoggingLevel.Normal) _logger.WriteLine("Failed to serialize protocols:  " + ex.Message); }
        }

        public void DestroyService()
        {
            _registeredProtocols = null;

            _logger.Close();
            _logger = null;
        }
    }
}

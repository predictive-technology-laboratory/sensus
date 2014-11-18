using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Sensus
{
    public class SensusServiceHelper
    {
        private List<Protocol> _registeredProtocols;
        private Logger _logger;
        private readonly string _logPath;

        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> RegisteredProtocols
        {
            get { return _registeredProtocols; }
        }

        public SensusServiceHelper()
        {
            _registeredProtocols = new List<Protocol>();

            _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");

#if DEBUG
            _logger = new Logger(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif
        }

        public void RegisterProtocol(Protocol protocol)
        {
            lock (_registeredProtocols)
                if (!_registeredProtocols.Contains(protocol))
                    _registeredProtocols.Add(protocol);
        }

        public void UnregisterProtocol(Protocol protocol)
        {
            lock (_registeredProtocols)
                _registeredProtocols.Remove(protocol);
        }

        public void Stop()
        {
            _logger.Close();

            lock (_registeredProtocols)
                foreach (Protocol protocol in _registeredProtocols)
                    protocol.StopAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Sensus
{
    public class SensusServiceHelper
    {
        private List<Protocol> _startedProtocols;
        private Logger _logger;
        private readonly string _logPath;

        public Logger Logger
        {
            get { return _logger; }
        }

        public List<Protocol> StartedProtocols
        {
            get { return _startedProtocols; }
        }

        public SensusServiceHelper()
        {
            _startedProtocols = new List<Protocol>();

            _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");

#if DEBUG
            _logger = new Logger(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif
        }

        public void StartProtocol(Protocol protocol)
        {
            lock (_startedProtocols)
            {
                protocol.StartAsync();
                _startedProtocols.Add(protocol);
            }
        }

        public void StopProtocol(Protocol protocol)
        {
            lock (_startedProtocols)
            {
                protocol.StopAsync();
                _startedProtocols.Remove(protocol);
            }
        }

        public void Stop()
        {
            _logger.Close();

            lock (_startedProtocols)
                foreach (Protocol protocol in _startedProtocols.ToList())
                {
                    protocol.StopAsync();
                    _startedProtocols.Remove(protocol);
                }
        }
    }
}

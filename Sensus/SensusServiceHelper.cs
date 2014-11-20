using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Sensus
{
    public class SensusServiceHelper
    {
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
            get { return _registeredProtocols; }
        }

        public SensusServiceHelper()
        {
            _stopped = false;
            _registeredProtocols = new List<Protocol>();

            _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");

#if DEBUG
            _logger = new Logger(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            _logger = new Logger(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif
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

                    _logger.Close();
                });
        }
    }
}

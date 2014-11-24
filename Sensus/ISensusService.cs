using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus
{
    public interface ISensusService
    {
        IEnumerable<Protocol> RegisteredProtocols { get; }

        LoggingLevel LoggingLevel { get; }

        void Log(string message);

        void RegisterProtocol(Protocol protocol);

        void StartProtocol(Protocol protocol);

        void StopProtocol(Protocol protocol, bool unregister);

        Task StopAsync();
    }
}

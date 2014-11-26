using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus
{
    /// <summary>
    /// Interface for platform-specific Sensus services. These typically need to derive from platform-specific classes (e.g., Service on Android),
    /// so we can't use an abstract base class to provide common functionality, e.g., that provided by SensusServiceHelper.
    /// </summary>
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

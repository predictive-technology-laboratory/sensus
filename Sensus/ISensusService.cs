using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus
{
    public interface ISensusService
    {
        Logger Logger { get; }

        IEnumerable<Protocol> RegisteredProtocols { get; }

        void StartProtocol(Protocol protocol);

        void StopProtocol(Protocol protocol);

        void Stop();
    }
}

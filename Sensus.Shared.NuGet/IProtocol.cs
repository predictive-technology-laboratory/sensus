using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sensus.Probes;

namespace Sensus
{
    public interface IProtocol
    {
        ProtocolState State { get; }

        JObject AgentPolicy { get; set; }

        Task UpdateScriptAgentPolicyAsync(CancellationToken cancellationToken);

        Task UpdateSensingAgentPolicyAsync(CancellationToken cancellationToken);

        bool TryGetProbe<DatumInterface, ProbeType>(out ProbeType probe) where DatumInterface : IDatum
                                                                         where ProbeType : class, IProbe;
    }
}
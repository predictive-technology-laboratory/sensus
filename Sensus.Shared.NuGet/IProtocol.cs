using System.Threading;
using System.Threading.Tasks;
using Sensus.Probes;

namespace Sensus
{
    public interface IProtocol
    {
        ProtocolState State { get; }

        Task UpdateScriptAgentPolicyAsync(CancellationToken cancellationToken);

        Task UpdateSensingAgentPolicyAsync(CancellationToken cancellationToken);

        bool TryGetProbe<DatumInterface, ProbeType>(out ProbeType probe) where DatumInterface : IDatum
                                                                         where ProbeType : class, IProbe;
    }
}
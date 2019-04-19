using System.Threading;
using System.Threading.Tasks;

namespace Sensus
{
    public interface IProtocol
    {
        ProtocolState State { get; }

        Task UpdateScriptAgentPolicyAsync(CancellationToken cancellationToken);

        Task UpdateSensingAgentPolicyAsync(CancellationToken cancellationToken);
    }
}
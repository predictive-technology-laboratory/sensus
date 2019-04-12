using System.Threading;
using System.Threading.Tasks;

namespace Sensus
{
    public interface IProtocol
    {
        Task UpdateScriptAgentPolicyAsync(CancellationToken cancellationToken);

        Task UpdateSensingAgentPolicyAsync(CancellationToken cancellationToken);
    }
}
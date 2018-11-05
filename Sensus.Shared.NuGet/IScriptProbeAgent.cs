using System.Threading.Tasks;

namespace Sensus.Probes.User.Scripts
{
    public interface IScriptProbeAgent
    {
        string Name { get; }
        string Id { get; }

        void Observe(IDatum datum);

        Task<bool> ShouldDeliverSurvey(IScript script);
    }
}
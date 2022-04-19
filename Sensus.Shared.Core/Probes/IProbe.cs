using System.Threading.Tasks;

namespace Sensus.Probes
{
    public interface IProbe
    {
        Task RestartAsync();
        Task StartAsync();
        Task StopAsync();
    }
}
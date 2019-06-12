using System.Threading.Tasks;

namespace Sensus
{
    public interface ISensusServiceHelper
    {
        ILogger Logger { get; }

        Task FlashNotificationAsync(string message);

        Task KeepDeviceAwakeAsync();

        Task LetDeviceSleepAsync();

        Task SaveAsync();
    }
}
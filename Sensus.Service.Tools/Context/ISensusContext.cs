using Sensus.Tools;

namespace Sensus.Service.Tools.Context
{
    public interface ISensusContext
    {
        Platform Platform { get; }
        IConcurrent MainThreadSynchronizer { get; }
    }
}

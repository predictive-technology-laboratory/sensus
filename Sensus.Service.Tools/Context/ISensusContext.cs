using Sensus.Tools;

namespace Sensus.Service.Tools.Context
{
    public interface ISensusContext
    {
        IConcurrent MainThreadSynchronizer { get; }
    }
}

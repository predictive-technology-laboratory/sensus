using Sensus.iOS.Concurrent;
using Sensus.Service.Tools.Context;
using Sensus.Tools;

namespace Sensus.Service.iOS.Context
{
    public class iOSSensusContext: ISensusContext
    {
        public iOSSensusContext()
        {
            MainThreadSynchronizer = new MainConcurrent();
        }

        public IConcurrent MainThreadSynchronizer { get; }
    }
}

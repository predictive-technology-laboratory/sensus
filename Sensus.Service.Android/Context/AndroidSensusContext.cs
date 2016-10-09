using Sensus.Android.Tools;
using Sensus.Service.Tools.Context;
using Sensus.Tools;

namespace Sensus.Service.Android.Context
{
    public class AndroidSensusContext: ISensusContext
    {
        public AndroidSensusContext()
        {
            MainThreadSynchronizer = new MainConcurrent();
        }

        public IConcurrent MainThreadSynchronizer { get; }
    }
}

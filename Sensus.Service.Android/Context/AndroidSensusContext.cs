using Sensus.Android.Tools;
using Sensus.Service.Tools.Context;
using Sensus.Tools;

namespace Sensus.Service.Android.Context
{
    public class AndroidSensusContext: ISensusContext
    {
        public AndroidSensusContext()
        {
            Platform               = Platform.Android;
            MainThreadSynchronizer = new MainConcurrent();
        }

        public Platform Platform { get; }

        public IConcurrent MainThreadSynchronizer { get; }
    }
}

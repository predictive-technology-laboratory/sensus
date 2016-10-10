using Sensus.Tools;
using Sensus.Service.Tools.Context;


namespace Sensus.Service.iOS.Context
{
    public class TestSensusContext: ISensusContext
    {
        public TestSensusContext()
        {
            Platform               = Platform.Test;
            MainThreadSynchronizer = new LockConcurrent();
        }

        public Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; set; }
    }
}

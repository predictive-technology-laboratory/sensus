using Sensus.Tools;
using Sensus.Service.Tools.Context;


namespace Sensus.Service.iOS.Context
{
    public class TestSensusContext: ISensusContext
    {
        public TestSensusContext()
        {
            MainThreadSynchronizer = new LockConcurrent();
        }

        public IConcurrent MainThreadSynchronizer { get; set; }
    }
}

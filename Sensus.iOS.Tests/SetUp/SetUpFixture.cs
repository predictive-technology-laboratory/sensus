using Sensus.Context;
using Sensus.Tests.Classes;
using Sensus.iOS.Concurrent;

namespace Sensus.iOS.Tests.SetUp
{   
    public static class SetUpFixture
    {
        public static void SetUp()
        {
            SensusContext.Current = new TestSensusContext
            {
                MainThreadSynchronizer = new MainConcurrent()
            };
        }
    }
}
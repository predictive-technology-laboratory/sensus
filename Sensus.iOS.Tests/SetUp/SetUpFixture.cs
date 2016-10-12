using Sensus.Shared.Context;
using Sensus.Shared.Test.Classes;
using Sensus.Shared.iOS.Concurrent;

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
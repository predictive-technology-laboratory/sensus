using Sensus.Shared.Context;
using Sensus.Shared.Test.Classes;
using Sensus.Shared.Android.Concurrent;

namespace Sensus.Android.Tests.SetUp
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
using Sensus.Context;
using Sensus.Test.Classes;
using Sensus.Android.Concurrent;

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
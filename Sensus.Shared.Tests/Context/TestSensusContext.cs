using Sensus.Service.Tools;
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
            Encryption             = new SimpleEncryption("");
        }

        public Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; set; }

        public IEncryption Encryption { get; set; }

    }
}

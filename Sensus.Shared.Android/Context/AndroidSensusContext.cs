using Sensus.Tools;
using Sensus.Android.Tools;
using Sensus.Service.Tools;
using Sensus.Service.Tools.Context;

namespace Sensus.Service.Android.Context
{
    public class AndroidSensusContext: ISensusContext
    {
        public AndroidSensusContext(string encryptionKey)
        {
            Platform               = Platform.Android;
            MainThreadSynchronizer = new MainConcurrent();
            Encryption             = new SimpleEncryption(encryptionKey);
        }

        public Platform Platform { get; }

        public IConcurrent MainThreadSynchronizer { get; }

        public IEncryption Encryption { get; }
    }
}

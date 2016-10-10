using Sensus.Tools;
using Sensus.Service.Tools;
using Sensus.Service.Tools.Context;
using Sensus.iOS.Concurrent;

namespace Sensus.Service.iOS.Context
{
    public class iOSSensusContext: ISensusContext
    {
        #region Constructor
        public iOSSensusContext(string encryptionKey)
        {
            Platform               = Platform.iOS;
            MainThreadSynchronizer = new MainConcurrent();
            Encryption             = new SimpleEncryption(encryptionKey);
        }
        #endregion

        public Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; }
        public IEncryption Encryption { get; }
    }
}

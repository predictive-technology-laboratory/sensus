using Sensus.iOS.Concurrent;
using Sensus.Service.Tools.Context;
using Sensus.Tools;

namespace Sensus.Service.iOS.Context
{
    public class iOSSensusContext: ISensusContext
    {
        #region Constructor
        public iOSSensusContext()
        {
            Platform               = Platform.iOS;
            MainThreadSynchronizer = new MainConcurrent();
        }
        #endregion

        public Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; }
    }
}

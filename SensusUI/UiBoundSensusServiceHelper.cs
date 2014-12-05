using SensusService;
using SensusService.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SensusUI
{
    public static class UiBoundSensusServiceHelper
    {
        private static SensusServiceHelper _sensusServiceHelper;
        private static object _staticLockObject = new object();

        public static SensusServiceHelper Get()
        {
            // service helper be null for a brief period between the time when the app starts and when the service connector binds.
            int triesLeft = 5;
            while (triesLeft-- > 0)
            {
                lock (_staticLockObject)
                    if (_sensusServiceHelper == null)
                        Thread.Sleep(1000);
                    else
                        break;
            }

            if (_sensusServiceHelper == null)
                throw new SensusException("Sensus UI failed to bind to service.");

            return _sensusServiceHelper;
        }

        public static void Set(SensusServiceHelper value)
        {
            lock (_staticLockObject)
            {
                _sensusServiceHelper = value;
            }
        }
    }
}

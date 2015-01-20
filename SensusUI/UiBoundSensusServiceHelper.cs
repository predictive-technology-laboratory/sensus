#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
 
using SensusService;
using SensusService.Exceptions;
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

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

using SensusService;
using SensusService.Exceptions;
using System.Threading;

namespace SensusUI
{
    /// <summary>
    /// Provides a means for the UI to access the underlying service helper object. This is set when the app starts
    /// up. Some platforms (e.g., Android) formally separate the UI from the model classes. In such cases, there can
    /// be a delay from app startup to the time at which the model service helper is bound. This class will allow
    /// UI operations to wait until the model service helper is bound.
    /// </summary>
    public static class UiBoundSensusServiceHelper
    {
        private static SensusServiceHelper SENSUS_SERVICE_HELPER;
        private static object GET_SENSUS_SERVICE_HELPER_LOCKER = new object();
        private static ManualResetEvent SENSUS_SERVICE_HELPER_WAIT = new ManualResetEvent(false);

        public static SensusServiceHelper Get(bool wait)
        {
            lock (GET_SENSUS_SERVICE_HELPER_LOCKER)
            {
                // the helper might be set from another thread, so we can wait for it here.
                if (SENSUS_SERVICE_HELPER == null && wait && !SENSUS_SERVICE_HELPER_WAIT.WaitOne(30000))
                    throw new SensusException("Sensus UI failed to bind to service.");

                return SENSUS_SERVICE_HELPER;
            }
        }

        public static void Set(SensusServiceHelper sensusServiceHelper)
        {
            SENSUS_SERVICE_HELPER = sensusServiceHelper;

            // the helper is set to null, e.g., when the app is stopped.
            if (SENSUS_SERVICE_HELPER == null)
                SENSUS_SERVICE_HELPER_WAIT.Reset();
            else
                SENSUS_SERVICE_HELPER_WAIT.Set();
        }            
    }
}
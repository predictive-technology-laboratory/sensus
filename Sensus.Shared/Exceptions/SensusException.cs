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

using System;
using Microsoft.AppCenter.Crashes;

namespace Sensus.Exceptions
{
    /// <summary>
    /// Convenience class for creating and tracking errors via the app center.
    /// </summary>
    public class SensusException : Exception
    {
        /// <summary>
        /// Report the specified exception to the app center error tracker.
        /// </summary>
        /// <param name="exception">Exception to report.</param>
        public static void Report(Exception exception)
        {
            // the service helper isn't always initialized (e.g., upon startup), so make sure to catch any exceptions.
            string deviceId = null;
            try
            {
                SensusServiceHelper.Get().Logger.Log("Reporting Sensus exception:  " + exception.Message, LoggingLevel.Normal, typeof(SensusException));
                deviceId = SensusServiceHelper.Get().DeviceId;
            }
            catch (Exception)
            { }

            try
            {
                Crashes.TrackError(new SensusException("Device " + (deviceId ?? "[no ID]") + ":  " + exception.Message, exception));
            }
            catch (Exception ex)
            {
                // the service helper isn't always initialized (e.g., upon startup), so make sure to catch any exceptions.
                try
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to report Sensus exception:  " + ex.Message, LoggingLevel.Normal, typeof(SensusException));
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Report the specified message and an associated inner exception to the app center error tracker.
        /// </summary>
        /// <returns>The report.</returns>
        /// <param name="message">Message.</param>
        /// <param name="innerException">Inner exception.</param>
        public static SensusException Report(string message, Exception innerException = null)
        {
            // the service helper isn't always initialized (e.g., upon startup), so make sure to catch any exceptions.
            string deviceId = null;
            try
            {
                SensusServiceHelper.Get().Logger.Log("Reporting Sensus exception:  " + message, LoggingLevel.Normal, typeof(SensusException));
                deviceId = SensusServiceHelper.Get().DeviceId;
            }
            catch (Exception)
            { }

            SensusException exceptionToReport = null;

            try
            {
                exceptionToReport = new SensusException("Device " + (deviceId ?? "[no ID]") + ":  " + message, innerException);
                Crashes.TrackError(exceptionToReport);
            }
            catch (Exception ex)
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to report exception:  " + ex.Message, LoggingLevel.Normal, typeof(SensusException));
                }
                catch (Exception)
                { }
            }

            return exceptionToReport;
        }

        protected SensusException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
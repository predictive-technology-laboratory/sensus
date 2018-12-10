//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

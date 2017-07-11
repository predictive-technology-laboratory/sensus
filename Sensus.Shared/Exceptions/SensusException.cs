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
using Xamarin;

namespace Sensus.Exceptions
{
    public class SensusException : Exception
    {
        public static SensusException Report(string message, Exception innerException = null)
        {
            SensusServiceHelper.Get().Logger.Log("Sensus exception being reported:  " + message + Environment.NewLine + "Stack:  " + Environment.StackTrace, LoggingLevel.Normal, typeof(SensusException));

            SensusException sensusException = new SensusException(message, innerException);

            try
            {
                Insights.Report(sensusException, "Stack Trace", Environment.StackTrace, Insights.Severity.Critical);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to report new exception to Xamarin Insights:  " + ex.Message, LoggingLevel.Normal, typeof(SensusException));
            }

            return sensusException;
        }

        protected SensusException(string message, Exception innerException = null)
            : base(message, innerException)
        {            
        }
    }
}
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

namespace SensusService.Exceptions
{
    public class SensusException : Exception
    {
        public SensusException(string message)
            : base(message)
        {
            SensusServiceHelper.Get().Logger.Log("Exception being created:  " + message + Environment.NewLine + "Stack:  " + Environment.StackTrace, LoggingLevel.Normal, GetType());

            try { Insights.Report(this, "Stack Trace", Environment.StackTrace, Xamarin.Insights.Severity.Error); }
            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to report new exception to Xamarin Insights:  " + ex.Message, LoggingLevel.Normal, GetType()); }
        }
    }
}
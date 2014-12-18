using System;
using Xamarin;

namespace SensusService.Exceptions
{
    public class SensusException : Exception
    {
        public SensusException(string message)
            : base(message)
        {
            SensusServiceHelper.Get().Logger.Log("Exception being created:  " + message + Environment.NewLine + "Stack:  " + StackTrace, LoggingLevel.Normal);
            Insights.Report(this);
        }
    }
}

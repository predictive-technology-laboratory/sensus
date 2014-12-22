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

            try { Insights.Report(this, ReportSeverity.Error); }
            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to report new exception to Xamarin Insights:  " + ex.Message, LoggingLevel.Normal); }
        }
    }
}

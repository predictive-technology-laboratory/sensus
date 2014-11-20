using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Exceptions
{
    public class SensusException : Exception
    {
        public SensusException(string message)
            : base(message)
        {
            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Exception being created:  " + message + Environment.NewLine + "Stack:  " + StackTrace);
        }
    }
}

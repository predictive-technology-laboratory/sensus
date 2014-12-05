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
            if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Normal)
                SensusServiceHelper.Get().Log("Exception being created:  " + message + Environment.NewLine + "Stack:  " + StackTrace);
        }
    }
}

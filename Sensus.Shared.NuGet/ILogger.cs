using System;

namespace Sensus
{
    public interface ILogger
    {
        LoggingLevel Level { get; set; }

        void Log(string message, LoggingLevel level, Type callingType, bool throwException = false);
    }
}
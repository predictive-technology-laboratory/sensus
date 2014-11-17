using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Exceptions
{
    public class DataStoreException : SensusException
    {
        public DataStoreException(string message)
            : base(message)
        {
        }
    }
}

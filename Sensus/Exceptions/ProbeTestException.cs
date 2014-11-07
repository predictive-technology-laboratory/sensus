using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Exceptions
{
    public class ProbeTestException : ProbeException
    {
        public ProbeTestException(Probe probe, string error)
            : base(probe, "Probe test failed:  " + error)
        {

        }
    }
}

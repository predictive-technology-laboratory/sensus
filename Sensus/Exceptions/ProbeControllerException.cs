using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Exceptions
{
    public class ProbeControllerException : SensusException
    {
        public ProbeController _controller;

        public ProbeControllerException(ProbeController controller, string message)
            : base(message)
        {
            _controller = controller;
        }
    }
}

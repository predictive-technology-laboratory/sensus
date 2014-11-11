using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Parameters
{
    public class BooleanProbeParameter :ProbeParameter
    {
        public BooleanProbeParameter(string labelText, bool editable)
            : base(labelText, editable)
        {
        }
    }
}

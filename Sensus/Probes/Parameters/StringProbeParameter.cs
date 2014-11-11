using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Parameters
{
    public class StringProbeParameter : ProbeParameter
    {
        public StringProbeParameter(string labelText, bool editable)
            : base(labelText, editable)
        {
        }
    }
}

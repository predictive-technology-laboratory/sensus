using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Location
{
    [Serializable]
    public abstract class CompassProbe : PassiveProbe
    {
        protected override string DisplayName
        {
            get { return "Compass"; }
        }
    }
}

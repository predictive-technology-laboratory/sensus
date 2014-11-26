using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Location
{
    public abstract class CompassProbe : ListeningProbe
    {
        protected override string DisplayName
        {
            get { return "Compass"; }
        }
    }
}

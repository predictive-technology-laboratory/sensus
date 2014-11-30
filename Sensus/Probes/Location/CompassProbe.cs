using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Location
{
    public abstract class CompassProbe : ListeningProbe
    {
        protected override int Id
        {
            get { return 2; }
        }

        protected override string DefaultDisplayName
        {
            get { return "Compass"; }
        }
    }
}

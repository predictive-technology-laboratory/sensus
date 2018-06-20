using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Device
{
    public abstract class PowerConnectionChange
    {
        public EventHandler<bool> POWER_CONNECTION_CHANGE;
    }
}

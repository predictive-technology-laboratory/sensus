using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Movement
{
    public interface IMagnetometerDatum: IDatum
    {
        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }
    }
}

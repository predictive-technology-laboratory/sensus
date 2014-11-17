using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    public interface IProbe
    {
        string Name { get; }

        void StoreDatum(Datum datum);
    }
}

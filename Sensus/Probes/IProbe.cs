using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sensus.Probes
{
    public interface IProbe
    {
        string Name { get; }

        void StoreDatum(Datum datum);

        void OnPropertyChanged([CallerMemberName] string propertyName = null);
    }
}

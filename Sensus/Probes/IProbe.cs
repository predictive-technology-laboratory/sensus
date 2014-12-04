using System.Runtime.CompilerServices;

namespace Sensus.Probes
{
    public interface IProbe
    {
        string DisplayName { get; }

        void StoreDatum(Datum datum);

        void OnPropertyChanged([CallerMemberName] string propertyName = null);
    }
}

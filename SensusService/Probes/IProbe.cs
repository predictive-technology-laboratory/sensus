using System.Runtime.CompilerServices;

namespace SensusService.Probes
{
    public interface IProbe
    {
        bool Enabled { get; }

        Protocol Protocol { get; }

        string DisplayName { get; }

        Datum MostRecentlyStoredDatum { get; }

        void StoreDatum(Datum datum);

        void OnPropertyChanged([CallerMemberName] string propertyName = null);
    }
}

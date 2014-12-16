using System.Runtime.CompilerServices;

namespace SensusService.Probes
{
    public interface IProbe
    {
        string DisplayName { get; }

        Datum MostRecentlyStoredDatum { get; }

        void StoreDatum(Datum datum);

        void OnPropertyChanged([CallerMemberName] string propertyName = null);
    }
}

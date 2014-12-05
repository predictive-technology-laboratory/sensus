using SensusService;
using SensusService.DataStores;

namespace SensusUI
{
    public class ProtocolDataStoreEventArgs
    {
        public Protocol Protocol { get; set; }

        public DataStore DataStore { get; set; }

        public bool Local { get; set; }
    }
}

using Sensus.DataStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.UI
{
    public class ProtocolDataStoreEventArgs
    {
        public Protocol Protocol { get; set; }

        public DataStore DataStore { get; set; }

        public bool Local { get; set; }
    }
}

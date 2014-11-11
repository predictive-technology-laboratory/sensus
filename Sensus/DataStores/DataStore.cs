using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores
{
    /// <summary>
    /// A repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        public abstract void Test();

        public abstract void Store(IEnumerable<Datum> data);
    }
}

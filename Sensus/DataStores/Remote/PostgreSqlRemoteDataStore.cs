using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores.Remote
{
    public class PostgreSqlRemoteDataStore : RemoteDataStore
    {
        public override void Test()
        {
            throw new NotImplementedException();
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }
    }
}

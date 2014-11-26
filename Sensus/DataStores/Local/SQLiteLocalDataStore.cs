using System;
using System.Collections.Generic;

namespace Sensus.DataStores.Local
{
    public class SQLiteLocalDataStore : LocalDataStore
    {
        protected override string DisplayName
        {
            get { return "SQL Lite"; }
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }

        public override ICollection<Datum> GetDataForRemoteDataStore()
        {
            throw new NotImplementedException();
        }

        public override void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }
    }
}

using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.DataStores.Local
{
    public class SQLiteLocalDataStore : LocalDataStore
    {
        protected override string DisplayName
        {
            get { return "Local SQL Lite"; }
        }

        public override void Test()
        {
            throw new NotImplementedException();
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

        protected override string DisplayName
        {
            get { return "Local RAM"; }
        }

        public RamLocalDataStore()
        {
            _data = new HashSet<Datum>();
        }

        public override void Test()
        {
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committed = new List<Datum>();
            lock(_data)
                foreach (Datum d in data)
                {
                    _data.Add(d);
                    committed.Add(d);
                }

            return committed;
        }

        public override ICollection<Datum> GetDataForRemoteDataStore()
        {
            return _data;
        }

        public override void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data)
        {
            lock (_data)
                foreach (Datum d in data)
                    _data.Remove(d);
        }
    }
}

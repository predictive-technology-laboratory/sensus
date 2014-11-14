using System.Collections.Generic;

namespace Sensus.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

        protected override string DisplayName
        {
            get { return "RAM"; }
        }

        public RamLocalDataStore()
        {
            _data = new HashSet<Datum>();
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committed = new List<Datum>();
            lock(_data)
                foreach (Datum datum in data)
                {
                    _data.Add(datum);
                    committed.Add(datum);
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

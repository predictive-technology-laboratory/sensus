using System;
using System.Collections.Generic;
using System.Linq;

namespace Sensus.DataStores.Local
{
    [Serializable]
    public class RamLocalDataStore : LocalDataStore
    {
        [NonSerialized]
        private HashSet<Datum> _data;

        protected override string DisplayName
        {
            get { return "RAM"; }
        }

        public RamLocalDataStore()
        {
        }

        protected override void Start()
        {
            _data = new HashSet<Datum>();

            base.Start();
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
            lock (_data)
                return _data.ToList();
        }

        public override void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data)
        {
            lock (_data)
                foreach (Datum d in data)
                    _data.Remove(d);
        }
    }
}

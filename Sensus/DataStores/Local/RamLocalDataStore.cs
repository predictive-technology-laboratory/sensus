using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sensus.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

        protected override string DisplayName
        {
            get { return "RAM"; }
        }

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StartAsync();

                    _data = new HashSet<Datum>();
                });
        }

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            return Task.Run<ICollection<Datum>>(() =>
                {
                    List<Datum> committed = new List<Datum>();

                    lock (_data)
                        foreach (Datum datum in data)
                        {
                            _data.Add(datum);
                            committed.Add(datum);
                        }

                    return committed;
                });
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

        public override void Clear()
        {
            lock (_data)
                _data.Clear();
        }
    }
}

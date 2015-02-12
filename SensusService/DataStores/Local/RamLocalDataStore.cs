#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SensusService.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

        private readonly object _locker = new object();

        protected override string DisplayName
        {
            get { return "RAM"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        public override void Start()
        {
            lock (_locker)
            {
                _data = new HashSet<Datum>();

                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data)
        {
            List<Datum> committed = new List<Datum>();

            lock (_data)
                foreach (Datum datum in data)
                {
                    _data.Add(datum);
                    committed.Add(datum);
                }

            return committed;
        }

        public override List<Datum> GetDataForRemoteDataStore()
        {
            lock (_data)
                return _data.ToList();
        }

        public override void ClearDataCommittedToRemoteDataStore(List<Datum> data)
        {
            lock (_data)
                foreach (Datum d in data)
                    _data.Remove(d);
        }

        public override void Clear()
        {
            if (_data != null)
                lock (_data)
                    _data.Clear();
        }

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();

                Clear();
            }
        }
    }
}

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

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using SensusUI.UiProperties;
using System.Threading.Tasks;
using System.IO;

namespace SensusService.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

        private readonly object _locker = new object();

        [JsonIgnore]
        public override string DisplayName
        {
            get { return "RAM"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return true; }
        }

        [JsonIgnore]
        public int DataCount
        {
            get
            {
                lock (_data)
                    return _data.Count; 
            }
        }

        public RamLocalDataStore()
        {
            _data = new HashSet<Datum>();
        }

        public override void Start()
        {
            lock (_locker)
            {
                _data.Clear();

                base.Start();
            }
        }

        protected override Task<List<Datum>> CommitDataAsync(List<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    List<Datum> committed = new List<Datum>();

                    lock (_data)
                        foreach (Datum datum in data)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            // all locally stored data, whether on disk or in RAM, should be anonymized as required
                            // by the protocol. convert datum to/from JSON in order to apply anonymization.

                            try
                            {
                                string json = datum.GetJSON(Protocol.JsonAnonymizer, false);

                                try
                                {
                                    Datum anonymizedDatum = Datum.FromJSON(json);

                                    try
                                    {
                                        _data.Add(anonymizedDatum);
                                        committed.Add(datum);
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Failed to add anonymized datum to collection:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to get datum from anonymized JSON:  " + ex.Message, LoggingLevel.Normal, GetType());
                                }
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to get anonymized JSON from datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }

                    return committed;
                });
        }

        public override List<Datum> GetDataForRemoteDataStore(CancellationToken cancellationToken)
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

        protected override IEnumerable<Tuple<string, string>> GetDataLinesToWrite(CancellationToken cancellationToken, Action<string, double> progressCallback)
        {
            lock (_data)
            {
                int count = 0;

                foreach (Datum datum in _data)
                {
                    if (progressCallback != null && _data.Count >= 10 && (count % (_data.Count / 10)) == 0)
                        progressCallback(null, count / (double)_data.Count);

                    yield return new Tuple<string, string>(datum.GetType().Name, datum.GetJSON(Protocol.JsonAnonymizer, false));

                    ++count;
                }

                if (progressCallback != null)
                    progressCallback(null, 1);
            }
        }

        public override void Clear()
        {
            if (_data != null)
                lock (_data)
                    _data.Clear();
        }

        public override void ClearForSharing()
        {
            base.ClearForSharing();

            _data.Clear();
        }
    }
}
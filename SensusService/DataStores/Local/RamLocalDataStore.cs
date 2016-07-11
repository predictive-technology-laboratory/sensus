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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.DataStores.Local
{
    public class RamLocalDataStore : LocalDataStore
    {
        private HashSet<Datum> _data;

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
        public override string SizeDescription
        {
            get
            {
                return CommittedDataCount + " items";
            }
        }

        public RamLocalDataStore()
        {
            _data = new HashSet<Datum>();
        }

        public override Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    lock (_data)
                    {
                        List<Datum> committedData = new List<Datum>();

                        foreach (Datum datum in data)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            // all locally stored data, whether on disk or in RAM, should be anonymized as required
                            // by the protocol. convert datum to/from JSON in order to apply anonymization.
                            try
                            {
                                string anonymizedDatumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);
                                Datum anonymizedDatum = Datum.FromJSON(anonymizedDatumJSON);
                                _data.Add(anonymizedDatum);
                                MostRecentSuccessfulCommitTime = DateTime.Now;
                                committedData.Add(anonymizedDatum);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to add anonymized datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }

                        return committedData;
                    }
                });
        }

        public override void CommitDataToRemoteDataStore(CancellationToken cancellationToken)
        {
            CommitChunksAsync(_data, Protocol.RemoteDataStore, cancellationToken).Wait();
        }

        protected override IEnumerable<Tuple<string, string>> GetDataLinesToWrite(CancellationToken cancellationToken, Action<string, double> progressCallback)
        {
            lock (_data)
            {
                int count = 0;

                foreach (Datum datum in _data)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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
            base.Clear();

            lock (_data)
            {
                _data.Clear();
            }
        }

        public override void ClearForSharing()
        {
            base.ClearForSharing();

            lock (_data)
            {
                _data.Clear();
            }
        }
    }
}
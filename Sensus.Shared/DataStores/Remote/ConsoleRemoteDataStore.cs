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
using System.Threading;
using Sensus.UI.UiProperties;
using System;
using System.Threading.Tasks;

namespace Sensus.DataStores.Remote
{
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        [JsonIgnore]
        public override string DisplayName
        {
            get { return "Console"; }
        }

        [JsonIgnore]
        public override bool CanRetrieveCommittedData
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return false; }
        }

        public override Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                List<Datum> committedData = new List<Datum>();

                foreach (Datum datum in data)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    SensusServiceHelper.Get().Logger.Log("Committed datum to remote console:  " + datum, LoggingLevel.Debug, GetType());
                    MostRecentSuccessfulCommitTime = DateTime.Now;
                    committedData.Add(datum);
                }

                return committedData;
            });
        }

        public override string GetDatumKey(Datum datum)
        {
            throw new Exception("Cannot retrieve datum key from Console Remote Data Store.");
        }

        public override Task<T> GetDatum<T>(string datumKey, CancellationToken cancellationToken)
        {
            throw new Exception("Cannot retrieve datum from Console Remote Data Store.");
        }
    }
}
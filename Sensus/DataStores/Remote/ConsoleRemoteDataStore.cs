using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.DataStores.Remote
{
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        protected override string DisplayName
        {
            get { return "Console"; }
        }

        [JsonIgnore]
        public override bool CanClear
        {
            get { return false; }
        }

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            return Task.Run<ICollection<Datum>>(() =>
                {
                    List<Datum> committedData = new List<Datum>();
                    foreach (Datum datum in data)
                    {
                        committedData.Add(datum);

                        if (App.LoggingLevel >= LoggingLevel.Debug)
                            App.Get().SensusService.Log("Committed datum to remote console:  " + datum);
                    }

                    return committedData;
                });
        }
    }
}

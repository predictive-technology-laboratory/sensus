using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensusService.DataStores.Remote
{
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        protected override string DisplayName
        {
            get { return "Console"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return false; }
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committedData = new List<Datum>();
            foreach (Datum datum in data)
            {
                committedData.Add(datum);

                SensusServiceHelper.Get().Logger.Log("Committed datum to remote console:  " + datum, LoggingLevel.Debug);
            }

            return committedData;
        }
    }
}

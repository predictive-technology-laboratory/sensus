using System;
using System.Collections.Generic;

namespace Sensus.DataStores.Remote
{
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        protected override string DisplayName
        {
            get { return "Console"; }
        }

        public ConsoleRemoteDataStore()
        {
            CommitDelayMS = 10000; // 10 seconds...so we can see debugging output
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committedData = new List<Datum>();
            foreach (Datum datum in data)
            {
                committedData.Add(datum);

                if (Logger.Level >= LoggingLevel.Debug)
                    Logger.Log("Committed datum to remote console:  " + datum);
            }

            return committedData;
        }
    }
}

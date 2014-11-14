using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores.Remote
{
    public class ConsoleRemoteDataStore : RemoteDataStore
    {
        protected override string DisplayName
        {
            get { return "Console"; }
        }

        public override void Test()
        {
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committedData = new List<Datum>();
            foreach (Datum datum in data)
            {
                Console.Error.WriteLine("Committed datum to remote console:  " + datum);
                committedData.Add(datum);
            }

            return committedData;
        }
    }
}

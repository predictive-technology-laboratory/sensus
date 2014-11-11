using Sensus.Probes;
using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.DataStores.Local
{
    public class SQLiteLocalDataStore : LocalDataStore
    {
        private Thread _thread;

        public override void Test()
        {
        }

        public override void Start(Protocol protocol)
        {
            _thread = new Thread(new ThreadStart(() =>
                {
                    while(protocol.Running)
                    {
                        CommitTrigger.WaitOne(CommitDelayMS);

                        foreach(Probe probe in protocol.Probes)
                            lock (probe.PolledData)
                            {
                                Store(probe.PolledData);
                                probe.PolledData.Clear();
                            }
                    }
                }));

            _thread.Start();
        }

        public override void Store(IEnumerable<Datum> data)
        {
            foreach (Datum datum in data)
                Console.Out.WriteLine(datum.ToString());
        }

        public override void Stop()
        {
            CommitTrigger.Set();
            _thread.Join();
        }
    }
}

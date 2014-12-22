using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.Communication
{
    public class CallDatum : Datum
    {
        private bool _incoming;
        private string _number;

        public CallDatum(Probe probe, DateTimeOffset timestamp, bool incoming, string number)
            : base(probe, timestamp)
        {
            _incoming = incoming;
            _number = number;
        }
    }
}

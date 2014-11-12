using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus
{
    /// <summary>
    /// A single unit of sensed information returned by a probe.
    /// </summary>
    public class Datum
    {
        private int _probeId;
        private DateTimeOffset _timestamp;

        public int ProbeId
        {
            get { return _probeId; }
        }

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
        }

        public Datum(int probeId, DateTimeOffset timestamp)
        {
            _probeId = probeId;
            _timestamp = timestamp;
        }
    }
}

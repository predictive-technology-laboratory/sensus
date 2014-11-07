using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus
{
    /// <summary>
    /// A single piece of sensed information returned by a probe.
    /// </summary>
    public class Datum
    {
        private DateTimeOffset _timestamp;

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
        }

        public Datum(DateTimeOffset timestamp)
        {
            _timestamp = timestamp;
        }
    }
}

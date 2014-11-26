using System;

namespace Sensus
{
    /// <summary>
    /// A single unit of sensed information returned by a probe.
    /// </summary>
    public abstract class Datum
    {
        private static int _datumNum = 0;

        private readonly int _id;
        private readonly int _hashCode;
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

        public abstract string DisplayDetail { get; }

        public Datum(int probeId, DateTimeOffset timestamp)
        {
            _id = _datumNum++;
            _hashCode = _id.GetHashCode();
            _probeId = probeId;
            _timestamp = timestamp;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            return (obj is Datum) && _id == (obj as Datum)._id;
        }

        public override string ToString()
        {
            return "Type:  " + GetType().Name + Environment.NewLine + 
                   "Id:  " + _id + Environment.NewLine +
                   "Probe:  " + _probeId + Environment.NewLine +
                   "Timestamp:  " + _timestamp;
        }
    }
}

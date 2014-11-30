using Newtonsoft.Json.Linq;
using System;

namespace Sensus
{
    /// <summary>
    /// A single unit of sensed information returned by a probe.
    /// </summary>
    public abstract class Datum
    {
        private int _userId;
        private int _probeId;
        private DateTimeOffset _timestamp;
        private int _hashCode;

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public int ProbeId
        {
            get { return _probeId; }
            set
            {
                _probeId = value;
                SetHashCode();
            }
        }

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set
            {
                _timestamp = value;
                SetHashCode();
            }
        }

        public abstract string DisplayDetail { get; }

        private Datum() { }  // for JSON.NET deserialization

        public Datum(int userId, int probeId, DateTimeOffset timestamp)
        {
            _userId = userId;
            _probeId = probeId;
            _timestamp = timestamp;

            SetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private void SetHashCode()
        {
            _hashCode = (_probeId + "-" + _timestamp).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Datum))
                return false;

            Datum d = obj as Datum;

            return (obj is Datum) && d._probeId == _probeId && d._timestamp == _timestamp;
        }

        public override string ToString()
        {
            return "Type:  " + GetType().Name + Environment.NewLine + 
                   "User ID:  " + _userId + Environment.NewLine +
                   "Probe:  " + _probeId + Environment.NewLine +
                   "Timestamp:  " + _timestamp;
        }
    }
}

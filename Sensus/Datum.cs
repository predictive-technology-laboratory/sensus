using Newtonsoft.Json.Linq;
using System;

namespace Sensus
{
    /// <summary>
    /// A single unit of sensed information returned by a probe.
    /// </summary>
    public abstract class Datum
    {
        private string _id;
        private int _userId;
        private int _probeId;
        private DateTimeOffset _timestamp;
        private int _hashCode;

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _hashCode = _id.GetHashCode();
            }
        }

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public int ProbeId
        {
            get { return _probeId; }
            set { _probeId = value; }
        }

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        public abstract string DisplayDetail { get; }

        private Datum() { }  // for JSON.NET deserialization

        public Datum(int userId, int probeId, DateTimeOffset timestamp)
        {
            _userId = userId;
            _probeId = probeId;
            _timestamp = timestamp;

            Id = userId + "-" + Guid.NewGuid().ToString();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Datum))
                return false;

            return (obj as Datum)._id == _id;
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

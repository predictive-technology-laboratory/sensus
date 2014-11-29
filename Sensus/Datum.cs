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
        private int _hashCode;
        private string _probeId;
        private DateTimeOffset _timestamp;

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _hashCode = _id.GetHashCode();
            }
        }

        public string ProbeId
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

        public Datum(string probeId, DateTimeOffset timestamp)
        {
            Id = Guid.NewGuid().ToString();
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

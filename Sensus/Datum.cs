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
        private long _timestampTicks;
        private int _hashCode;  // the hash code is based on combining the probe ID with the timestamp of datum generation

        /// <summary>
        /// Present for database storage purposes. This should never be set programmatically. Rather, it is set by the database upon insert.
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

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
                SetHashCode();  // the hash code is based on combining the probe ID with the timestamp of datum generation
            }
        }

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set
            {
                long newTimestampTicks = value.UtcTicks;
                if (newTimestampTicks != _timestampTicks)
                {
                    _timestampTicks = newTimestampTicks;
                    _timestamp = new DateTimeOffset(_timestampTicks, new TimeSpan());
                    SetHashCode();
                }
            }
        }

        public abstract string DisplayDetail { get; }

        private Datum() { }  // for JSON.NET deserialization

        public Datum(int userId, int probeId, long timestampTicks)
        {
            _userId = userId;
            _probeId = probeId;
            _timestampTicks = timestampTicks;
            _timestamp = new DateTimeOffset(_timestampTicks, new TimeSpan());

            SetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private void SetHashCode()
        {
            _hashCode = (_probeId + "-" + _timestampTicks).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Datum))
                return false;

            Datum d = obj as Datum;

            return (obj is Datum) && d._probeId == _probeId && d._timestampTicks == _timestampTicks;
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

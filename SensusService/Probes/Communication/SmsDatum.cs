using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace SensusService.Probes.Communication
{
    public class SmsDatum : Datum
    {
        private string _fromNumber;
        private string _toNumber;
        private string _message;

        public string FromNumber
        {
            get { return _fromNumber; }
            set { _fromNumber = value == null ? "" : new Regex(@"[^0-9]").Replace(value, ""); }
        }

        public string ToNumber
        {
            get { return _toNumber; }
            set { _toNumber = value == null ? "" : new Regex(@"[^0-9]").Replace(value, ""); }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _message; }
        }

        public SmsDatum(Probe probe, DateTimeOffset timestamp, string fromNumber, string toNumber, string message)
            : base(probe, timestamp)
        {
            FromNumber = fromNumber;
            ToNumber = toNumber;
            _message = message;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "From:  " + _fromNumber + Environment.NewLine +
                   "To:  " + _toNumber + Environment.NewLine +
                   "Message:  " + _message;
        }
    }
}

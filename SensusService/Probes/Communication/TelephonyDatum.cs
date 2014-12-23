using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Communication
{
    public class TelephonyDatum : Datum
    {
        private string _state;
        private string _incomingNumber;

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        public string IncomingNumber
        {
            get { return _incomingNumber; }
            set { _incomingNumber = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _incomingNumber + " (" + _state + ")"; }
        }

        public TelephonyDatum(Probe probe, DateTimeOffset timestamp, string state, string incomingNumber)
            : base(probe, timestamp)
        {
            _state = state;
            _incomingNumber = incomingNumber;
        }
    }
}

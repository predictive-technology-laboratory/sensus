using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Communication
{
    public class TelephonyDatum : Datum
    {
        private string _state;
        private string _phoneNumber;

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        public string PhoneNumber
        {
            get { return _phoneNumber; }
            set { _phoneNumber = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _phoneNumber + " (" + _state + ")"; }
        }

        public TelephonyDatum(Probe probe, DateTimeOffset timestamp, string state, string phoneNumber)
            : base(probe, timestamp)
        {
            _state = state;
            _phoneNumber = phoneNumber;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "State:  " + _state + Environment.NewLine +
                   "Number:  " + _phoneNumber;
        }
    }
}

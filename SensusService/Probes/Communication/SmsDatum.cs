using System;

namespace SensusService.Probes.Communication
{
    public class SmsDatum : Datum
    {
        private string _from;
        private string _to;
        private string _message;

        public string From
        {
            get { return _from; }
            set { _from = value; }
        }

        public string To
        {
            get { return _to; }
            set { _to = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public override string DisplayDetail
        {
            get { return _message; }
        }

        public SmsDatum(Probe probe, DateTimeOffset timestamp, string from, string to, string message)
            : base(probe, timestamp)
        {
            _from = from;
            _to = to;
            _message = message;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService
{
    public class ProtocolReport : Datum
    {
        private string _error;
        private string _warning;
        private string _misc;

        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        public string Warning
        {
            get { return _warning; }
            set { _warning = value; }
        }

        public string Misc
        {
            get { return _misc; }
            set { _misc = value; }
        }

        public override string DisplayDetail
        {
            get { return ""; }
        }

        public ProtocolReport(DateTimeOffset timestamp, string error, string warning, string misc)
            : base(null, timestamp)
        {
            _error = error;
            _warning = warning;
            _misc = misc;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService
{
    public class ProtocolReport
    {
        private string _id;
        private string _deviceId;
        private DateTimeOffset _timestamp;
        private string _error;
        private string _warning;
        private string _misc;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

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

        public ProtocolReport(string deviceId, DateTimeOffset timestamp, string error, string warning, string misc)
        {
            _id = Guid.NewGuid().ToString();
            _deviceId = deviceId;
            _timestamp = timestamp;
            _error = error;
            _warning = warning;
            _misc = misc;
        }
    }
}

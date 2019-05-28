using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Device
{
    class PowerConnectDatum : Datum
    {
        private bool _connection;

        
        public PowerConnectDatum(DateTimeOffset timestamp, bool connection) : base(timestamp)
        {
            _connection = connection;
        }


        public override string DisplayDetail
        {
            get
            {
                return "(Power connect Data)";
            }
        }

        public bool Connection { get => _connection; set => _connection = value; }

        public override object StringPlaceholderValue => throw new NotImplementedException();

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                       "Connected:  " + _connection+ Environment.NewLine;
        }
    }
}

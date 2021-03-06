﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Device
{
    class PowerConnectDatum : Datum
    {
        private bool _connection;
        private double _level;

        
        public PowerConnectDatum(DateTimeOffset timestamp, bool connection, double level) : base(timestamp)
        {
            _connection = connection;
            _level = level;
        }


        public override string DisplayDetail
        {
            get
            {
                return "(Power connection Data)";
            }
        }

        public bool Connection { get => _connection; set => _connection = value; }

        public double Level { get => _level; set => _level = value; }

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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.Context
{
    public class BluetoothDeviceProximityDatum : Datum
    {
        private string _name;
        private string _address;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Address
        {
            get { return _address; }
            set { _address = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _name + " (" + _address + ")"; }
        }

        public BluetoothDeviceProximityDatum(Probe probe, DateTimeOffset timestamp, string name, string address)
            : base(probe, timestamp)
        {
            _name = name;
            _address = address;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Name:  " + _name + Environment.NewLine +
                   "Address:  " + _address;
        }
    }
}

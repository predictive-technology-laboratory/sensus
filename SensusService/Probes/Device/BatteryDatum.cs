using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.Device
{
    public class BatteryDatum : Datum
    {
        private double _level;

        public double Level
        {
            get { return _level; }
            set { _level = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "Level:  " + _level; }
        }

        public BatteryDatum(Probe probe, DateTimeOffset timestamp, double level)
            : base(probe, timestamp)
        {
            _level = level;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Level:  " + _level;
        }
    }
}

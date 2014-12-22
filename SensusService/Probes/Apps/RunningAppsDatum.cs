using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.Apps
{
    public class RunningAppsDatum : Datum
    {
        private string _name;
        private string _description;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public override string DisplayDetail
        {
            get { return "Name:  " + _name + "; Description:  " + _description; }
        }

        public RunningAppsDatum(Probe probe, DateTimeOffset timestamp, string name, string description)
            : base(probe, timestamp)
        {
            _name = name;
            _description = description;
        }
    }
}

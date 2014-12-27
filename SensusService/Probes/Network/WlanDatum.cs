using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Network
{
    public class WlanDatum : Datum
    {
        private string _accessPoint;

        public string AccessPoint
        {
            get { return _accessPoint; }
            set { _accessPoint = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "Access Point:  " + _accessPoint; }
        }

        public WlanDatum(Probe probe, DateTimeOffset timestamp, string accessPoint)
            : base(probe, timestamp)
        {
            _accessPoint = accessPoint;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Access Point:  " + _accessPoint;
        }
    }
}

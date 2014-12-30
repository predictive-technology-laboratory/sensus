using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Network
{
    public class WlanDatum : Datum
    {
        private string _accessPointBSSID;

        public string AccessPointBSSID
        {
            get { return _accessPointBSSID; }
            set { _accessPointBSSID = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "AP BSSID:  " + _accessPointBSSID; }
        }

        public WlanDatum(Probe probe, DateTimeOffset timestamp, string accessPointBSSID)
            : base(probe, timestamp)
        {
            _accessPointBSSID = accessPointBSSID;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "AP BSSID:  " + _accessPointBSSID;
        }
    }
}

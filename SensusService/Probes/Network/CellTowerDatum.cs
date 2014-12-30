using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Network
{
    public class CellTowerDatum : Datum
    {
        private string _cellTower;

        public string CellTower
        {
            get { return _cellTower; }
            set { _cellTower = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _cellTower; }
        }

        public CellTowerDatum(Probe probe, DateTimeOffset timestamp, string cellTower)
            : base(probe, timestamp)
        {
            _cellTower = cellTower;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Cell Tower:  " + _cellTower;
        }
    }
}

using Android.Telephony;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerChangeListener : PhoneStateListener
    {
        public event EventHandler<string> CellTowerChanged;

        public override void OnCellLocationChanged(CellLocation location)
        {
            if (CellTowerChanged != null)
                CellTowerChanged(this, location.ToString());
        }
    }
}
using Android.Telephony;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerChangeListener : PhoneStateListener
    {
        private Action<string> _cellTowerChanged;

        public AndroidCellTowerChangeListener(Action<string> cellTowerChangedCallback)
        {
            _cellTowerChanged = cellTowerChangedCallback;
        }

        public override void OnCellLocationChanged(CellLocation location)
        {
            _cellTowerChanged(location.ToString());
        }
    }
}
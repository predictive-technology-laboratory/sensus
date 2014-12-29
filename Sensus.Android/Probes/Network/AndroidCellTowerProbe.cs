using Android.App;
using Android.Telephony;
using SensusService;
using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerProbe : CellTowerProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidCellTowerChangeListener _cellTowerChangeListener;

        public AndroidCellTowerProbe()
        {
            _cellTowerChangeListener = new AndroidCellTowerChangeListener();
            _cellTowerChangeListener.CellTowerChanged += (o, cellTowerLocation) =>
                {
                    StoreDatum(new CellTowerDatum(this, DateTimeOffset.UtcNow, cellTowerLocation));
                };
        }

        protected override void Initialize()
        {
            base.Initialize();

            _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
            if (_telephonyManager == null)
                throw new Exception("No telephony present.");
        }

        protected override void StartListening()
        {
            _telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.CellLocation);
        }

        protected override void StopListening()
        {
            _telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.None);
        }
    }
}
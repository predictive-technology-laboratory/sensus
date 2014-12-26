
using Android.App;
using Android.Content;
using Android.Telephony;
using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerProbe : CellTowerProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidPhoneStateListener _phoneStateListener;

        public AndroidCellTowerProbe()
        {
            _telephonyManager = Application.Context.GetSystemService(Context.TelephonyService) as TelephonyManager;
            _phoneStateListener = new AndroidPhoneStateListener();

            _phoneStateListener.CellLocationChanged += (o, e) =>
                {
                    StoreDatum(new CellTowerDatum(this, DateTimeOffset.UtcNow, e.ToString()));
                };
        }

        public override void StartListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.CellLocation);
        }

        public override void StopListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.None);
        }
    }
}
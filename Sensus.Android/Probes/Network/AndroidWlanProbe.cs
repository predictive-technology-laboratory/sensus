using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidWlanProbe : WlanProbe
    {
        private EventHandler<WlanDatum> _wlanConnectionChangedCallback;

        public AndroidWlanProbe()
        {
            _wlanConnectionChangedCallback = (sender, wlanDatum) =>
                {
                    wlanDatum.ProbeType = GetType().FullName;
                    StoreDatum(wlanDatum);
                };
        }

        protected override void StartListening()
        {
            AndroidWlanBroadcastReceiver.WifiConnectionChanged += _wlanConnectionChangedCallback;
        }

        protected override void StopListening()
        {
            AndroidWlanBroadcastReceiver.WifiConnectionChanged -= _wlanConnectionChangedCallback;
        }
    }
}
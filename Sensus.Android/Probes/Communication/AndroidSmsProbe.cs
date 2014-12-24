using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsProbe : SmsProbe
    {
        public override void StartListening()
        {
            AndroidSmsBroadcastReceiver.MessageSent += (o, m) =>
                {
                    StoreDatum(new SmsDatum(this, DateTimeOffset.UtcNow, m.OriginatingAddress, null, m.MessageBody));
                };

            AndroidSmsBroadcastReceiver.MessageReceived += (o, m) =>
                {
                    StoreDatum(new SmsDatum(this, DateTimeOffset.UtcNow, m.OriginatingAddress, null, m.MessageBody));
                };
        }

        public override void StopListening()
        {
            AndroidSmsBroadcastReceiver.Stop();
        }
    }
}
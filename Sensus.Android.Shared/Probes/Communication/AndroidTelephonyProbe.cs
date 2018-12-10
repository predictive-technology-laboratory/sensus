//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Android.App;
using Android.Telephony;
using Sensus.Probes.Communication;
using System;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidTelephonyProbe : ListeningTelephonyProbe
    {
        private TelephonyManager _telephonyManager;
        private EventHandler<string> _outgoingCallCallback;
        private AndroidTelephonyIdleIncomingListener _idleIncomingCallListener;
        private DateTime? _outgoingIncomingTime;

        public AndroidTelephonyProbe()
        {
            _outgoingCallCallback = async (sender, outgoingNumber) =>
            {
                _outgoingIncomingTime = DateTime.Now;

                await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.OutgoingCall, outgoingNumber, null));
            };

            _idleIncomingCallListener = new AndroidTelephonyIdleIncomingListener();

            _idleIncomingCallListener.IncomingCall += async (o, incomingNumber) =>
            {
                _outgoingIncomingTime = DateTime.Now;

                await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.IncomingCall, incomingNumber, null));
            };

            _idleIncomingCallListener.Idle += async (o, phoneNumber) =>
            {
                // only calculate call duration if we have previously received an incoming or outgoing call event (android might report idle upon startup)
                double? callDurationSeconds = null;
                if (_outgoingIncomingTime != null)
                {
                    callDurationSeconds = (DateTime.Now - _outgoingIncomingTime.Value).TotalSeconds;
                }

                await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.Idle, phoneNumber, callDurationSeconds));
            };
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Phone) == PermissionStatus.Granted)
            {
                _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
                if (_telephonyManager == null)
                {
                    throw new NotSupportedException("No telephony present.");
                }
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable phone in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Telephony not permitted on this device. Cannot start telephony probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override Task StartListeningAsync()
        {
            AndroidTelephonyOutgoingBroadcastReceiver.OUTGOING_CALL += _outgoingCallCallback;
            _telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.CallState);
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            AndroidTelephonyOutgoingBroadcastReceiver.OUTGOING_CALL -= _outgoingCallCallback;
            _telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.None);
            return Task.CompletedTask;
        }
    }
}

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

using System;
using Android.App;
using Android.Telephony;
using System.Threading.Tasks;
using Sensus.Probes.Communication;
using Plugin.Permissions.Abstractions;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsProbe : SmsProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidSmsOutgoingObserver _smsOutgoingObserver;
        private EventHandler<SmsDatum> _incomingSmsCallback;

        public AndroidSmsProbe()
        {
            _smsOutgoingObserver = new AndroidSmsOutgoingObserver(async outgoingSmsDatum =>
            {
                // the observer doesn't set the from number (current device)
                outgoingSmsDatum.FromNumber = _telephonyManager.Line1Number;

                await StoreDatumAsync(outgoingSmsDatum);
            });

            _incomingSmsCallback = async (sender, incomingSmsDatum) =>
            {
                // the observer doesn't set the destination number (simply the device's primary number)
                incomingSmsDatum.ToNumber = _telephonyManager.Line1Number;

                await StoreDatumAsync(incomingSmsDatum);
            };
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Sms) == PermissionStatus.Granted)
            {
                _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;

                if (_telephonyManager == null)
                {
                    throw new NotSupportedException("No telephony present.");
                }

                _smsOutgoingObserver.Initialize();
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable SMS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "SMS is not permitted on this device. Cannot start SMS probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override Task StartListeningAsync()
        {
            Application.Context.ContentResolver.RegisterContentObserver(global::Android.Net.Uri.Parse("content://sms"), true, _smsOutgoingObserver);
            Application.Context.ContentResolver.RegisterContentObserver(global::Android.Net.Uri.Parse("content://mms-sms"), true, _smsOutgoingObserver);

            AndroidSmsIncomingBroadcastReceiver.INCOMING_SMS += _incomingSmsCallback;

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            Application.Context.ContentResolver.UnregisterContentObserver(_smsOutgoingObserver);
            AndroidSmsIncomingBroadcastReceiver.INCOMING_SMS -= _incomingSmsCallback;
            return Task.CompletedTask;
        }
    }
}

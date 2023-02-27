// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Android.App;
using Android.Telephony;
using Sensus.Probes.Communication;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Plugin.ContactService.Shared;
using System.Text.RegularExpressions;
using Xamarin.Essentials;
using Contact = Plugin.ContactService.Shared.Contact;

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
				Contact contact = await SensusServiceHelper.GetContactAsync(outgoingNumber);
				bool isContact = contact != null;

				await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.OutgoingCall, outgoingNumber, null, isContact, contact?.Name, contact?.Email));
			};

			_idleIncomingCallListener = new AndroidTelephonyIdleIncomingListener();

			_idleIncomingCallListener.IncomingCall += async (o, incomingNumber) =>
			{
				_outgoingIncomingTime = DateTime.Now;
				Contact contact = await SensusServiceHelper.GetContactAsync(incomingNumber);
				bool isContact = contact != null;


				await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.IncomingCall, incomingNumber, null, isContact, contact?.Name, contact?.Email));
			};

			_idleIncomingCallListener.Idle += async (o, phoneNumber) =>
			{

				// only calculate call duration if we have previously received an incoming or outgoing call event (android might report idle upon startup)
				double? callDurationSeconds = null;
				if (_outgoingIncomingTime != null)
				{
					callDurationSeconds = (DateTime.Now - _outgoingIncomingTime.Value).TotalSeconds;
				}
				Contact contact = await SensusServiceHelper.GetContactAsync(phoneNumber);
				bool isContact = contact != null;

				await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, TelephonyState.Idle, phoneNumber, callDurationSeconds, isContact, contact?.Name, contact?.Email));
			};
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.Phone>() == PermissionStatus.Granted)
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

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			AndroidTelephonyOutgoingBroadcastReceiver.OUTGOING_CALL += _outgoingCallCallback;
			_telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.CallState);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			AndroidTelephonyOutgoingBroadcastReceiver.OUTGOING_CALL -= _outgoingCallCallback;
			_telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.None);
		}
	}
}

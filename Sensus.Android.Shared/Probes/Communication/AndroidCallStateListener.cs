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

using System;
using Android.App;
using Android.OS;
using Android.Telephony;
using Sensus.Probes.Communication;

namespace Sensus.Android.Probes.Communication
{
	public class AndroidCallStateListener
	{
		private readonly AndroidTelephonyProbe _probe;
		private readonly AndroidCallStateCallback _callback;
		private readonly AndroidPhoneStateListenerShim _listener;

		private class AndroidCallStateCallback : TelephonyCallback, TelephonyCallback.ICallStateListener
		{
			private readonly AndroidCallStateListener _listener;

			public AndroidCallStateCallback(AndroidCallStateListener listener)
			{
				_listener = listener;
			}

			public void OnCallStateChanged(int state)
			{
				_listener.OnCallStateChanged(state);
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		private class AndroidPhoneStateListenerShim : PhoneStateListener
		{
			private readonly AndroidCallStateListener _listener;

			public AndroidPhoneStateListenerShim(AndroidCallStateListener listener)
			{
				_listener = listener;
			}

			[Obsolete]
			public override void OnCallStateChanged(CallState state, string phoneNumber)
			{
				_listener.OnCallStateChanged((int)state);
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public AndroidCallStateListener(AndroidTelephonyProbe probe)
		{
			_probe = probe;

			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				_callback = new AndroidCallStateCallback(this);
			}
			else
			{
				_listener = new AndroidPhoneStateListenerShim(this);
			}
		}

		public void StartListening()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				AndroidSensusServiceHelper.TelephonyManager.RegisterTelephonyCallback(Application.Context.MainExecutor, _callback);
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				AndroidSensusServiceHelper.TelephonyManager.Listen(_listener, PhoneStateListenerFlags.CallState);
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public void StopListening()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				AndroidSensusServiceHelper.TelephonyManager.UnregisterTelephonyCallback(null);
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				AndroidSensusServiceHelper.TelephonyManager.Listen(_listener, PhoneStateListenerFlags.None);
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public async void OnCallStateChanged(int state)
		{
			await _probe.CreateDatumAsync((TelephonyState)state);
		}
	}
}

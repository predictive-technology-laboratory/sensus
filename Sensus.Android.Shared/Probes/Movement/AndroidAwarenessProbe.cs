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
using Android.Content;
using Android.Gms.Awareness;
using Android.Gms.Awareness.Fence;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Newtonsoft.Json;
using Sensus.Probes;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using AndroidTask = Android.Gms.Tasks.Task;
using Android.Gms.Tasks;
using Java.Util.Concurrent;
using Android.Gms.Extensions;

namespace Sensus.Android.Probes.Movement
{
	/// <summary>
	/// Base class for Google Awareness API probes.
	/// </summary>
	public abstract class AndroidAwarenessProbe : ListeningProbe
	{
		protected enum FenceUpdateAction
		{
			Add,
			Remove
		}

		public const string AWARENESS_PENDING_INTENT_ACTION = "SENSUS_AWARENESS_UPDATE";

		private AndroidAwarenessProbeBroadcastReceiver _awarenessBroadcastReceiver;
		private FenceClient _fenceClient;
		private PendingIntent _fencePendingIntent;

		[JsonIgnore]
		protected AndroidAwarenessProbeBroadcastReceiver AwarenessBroadcastReceiver
		{
			get
			{
				return _awarenessBroadcastReceiver;
			}
		}

		[JsonIgnore]
		protected FenceClient FenceClient
		{
			get
			{
				return _fenceClient;
			}
		}

		[JsonIgnore]
		protected override bool DefaultKeepDeviceAwake
		{
			get
			{
				return false;
			}
		}

		[JsonIgnore]
		protected override string DeviceAsleepWarning
		{
			get
			{
				return null;
			}
		}

		[JsonIgnore]
		protected override string DeviceAwakeWarning
		{
			get
			{
				return "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";
			}
		}

		public AndroidAwarenessProbe()
		{
			_awarenessBroadcastReceiver = new AndroidAwarenessProbeBroadcastReceiver();
			_fencePendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, new Intent(AWARENESS_PENDING_INTENT_ACTION), PendingIntentFlags.Immutable);
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			// check for availability of Google Play Services
			int googlePlayServicesAvailability = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Application.Context);

			if (googlePlayServicesAvailability != ConnectionResult.Success)
			{
				string message = "Google Play Services are not available on this device.";

				if (googlePlayServicesAvailability == ConnectionResult.ServiceVersionUpdateRequired)
				{
					message += " Please update your phone's Google Play Services app using the App Store. Then restart your study.";
				}
				else if (GoogleApiAvailability.Instance.IsUserResolvableError(googlePlayServicesAvailability))
				{
					message += " Please fix the following error and then restart your study:  " + GoogleApiAvailability.Instance.GetErrorString(googlePlayServicesAvailability);
				}

				message += " Email the study organizers and tell them you received the following error code:  " + googlePlayServicesAvailability;

				// the problem we've encountered is potentially fixable, so do not throw a NotSupportedException, as doing this would
				// disable the probe and prevent any future restart attempts from succeeding.
				throw new Exception(message);
			}

			_fenceClient = Awareness.GetFenceClient(Application.Context);
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			// register receiver for all awareness intent actions
			Application.Context.RegisterReceiver(_awarenessBroadcastReceiver, new IntentFilter(AWARENESS_PENDING_INTENT_ACTION));
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			// stop broadcast receiver
			Application.Context.UnregisterReceiver(_awarenessBroadcastReceiver);
		}

		protected void UpdateRequestBuilder(AwarenessFence fence, string fenceKey, FenceUpdateAction action, ref FenceUpdateRequestBuilder requestBuilder)
		{
			if (action == FenceUpdateAction.Add)
			{
				requestBuilder.AddFence(fenceKey, fence, _fencePendingIntent);
			}
			else if (action == FenceUpdateAction.Remove)
			{
				requestBuilder.RemoveFence(fenceKey);
			}
		}

		protected async Task<bool> UpdateFencesAsync(IFenceUpdateRequest updateRequest)
		{
			bool success = false;

			try
			{
				// we've seen cases where the update blocks indefinitely (e.g., due to outdated google play services on the 
				// phone). impose a timeout to avoid such blocks.
				AndroidTask updateFencesTask = TasksClass.WithTimeout(_fenceClient.UpdateFences(updateRequest), 60, TimeUnit.Seconds);

				await updateFencesTask;

				if (updateFencesTask.IsSuccessful)
				{
					SensusServiceHelper.Get().Logger.Log("Updated Google Awareness API fences.", LoggingLevel.Normal, GetType());
					success = true;
				}
				else if (updateFencesTask.IsCanceled)
				{
					SensusServiceHelper.Get().Logger.Log("Google Awareness API fence update canceled.", LoggingLevel.Normal, GetType());
				}
				else if (updateFencesTask.Exception != null)
				{
					throw updateFencesTask.Exception;
				}
			}
			// catch any errors from calling UpdateFences
			catch (Exception ex)
			{
				// ensure that wait is always set
				SensusServiceHelper.Get().Logger.Log("Exception while updating fences:  " + ex, LoggingLevel.Normal, GetType());
			}

			return success;
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			throw new NotImplementedException();
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override ChartSeries GetChartSeries()
		{
			throw new NotImplementedException();
		}
	}
}

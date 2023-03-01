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
using Newtonsoft.Json;
using Sensus.Probes.Location;
using Sensus.UI.UiProperties;
using System.Threading.Tasks;
using Android.Gms.Awareness.Snapshot;
using Android.Gms.Awareness;
using Android.Gms.Awareness.Fence;
using Xamarin.Essentials;
using Android.App;
using AndroidLocation = Android.Locations.Location;
using AndroidTask = Android.Gms.Tasks.Task;
using Android.Gms.Extensions;

namespace Sensus.Android.Probes.Movement
{
	/// <summary>
	/// Provides location fence information via the Google Awareness API as <see cref="LocationDatum"/> readings.
	/// </summary>
	public class AndroidLocationFenceProbe : AndroidAwarenessProbe
	{
		public const string AWARENESS_EXITING_LOCATION_FENCE_KEY = "EXITING_LOCATION_FENCE";

		private SnapshotClient _snapshotClient;
		private int _locationChangeRadiusMeters;

		/// <summary>
		/// How far in meters the device must travel before issuing a location-changed event.
		/// </summary>
		/// <value>The location change radius, in meters.</value>
		[EntryIntegerUiProperty("Location Change Radius (Meters):", true, 30, true)]
		public int LocationChangeRadiusMeters
		{
			get
			{
				return _locationChangeRadiusMeters;
			}
			set
			{
				if (value <= 0)
				{
					value = 100;
				}

				_locationChangeRadiusMeters = value;
			}
		}

		[JsonIgnore]
		public override Type DatumType
		{
			get
			{
				return typeof(LocationDatum);
			}
		}

		[JsonIgnore]
		public override string DisplayName
		{
			get
			{
				return "Location Fence";
			}
		}

		public AndroidLocationFenceProbe()
		{
			_locationChangeRadiusMeters = 100;

			AwarenessBroadcastReceiver.LocationChanged += async (sender, e) =>
			{
				await RequestLocationSnapshotAsync();
			};
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			// we need location permission in order to snapshot / fence the user's location.
			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() != PermissionStatus.Granted)
			{
				string error = "Geolocation is not permitted on this device. Cannot start location fences.";

				throw new Exception(error);
			}
			else
			{
				_snapshotClient = Awareness.GetSnapshotClient(Application.Context);
			}
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			// request a location to start location fencing
			await RequestLocationSnapshotAsync();
		}

		private async Task RequestLocationSnapshotAsync()
		{
			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() == PermissionStatus.Granted)
			{
				// store current location
				LocationResponse locationResponse = (await _snapshotClient.GetLocation()) as LocationResponse;
				AndroidLocation location = locationResponse.Location;
				DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time);
				await StoreDatumAsync(new LocationDatum(timestamp, location.HasAccuracy ? location.Accuracy : -1, location.Latitude, location.Longitude));
				// replace the previous location fence with one around the current location. additions and removals are handled
				// in the order specified below.
				FenceUpdateRequestBuilder locationFenceRequestBuilder = new FenceUpdateRequestBuilder();
				UpdateRequestBuilder(null, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Remove, ref locationFenceRequestBuilder);
				AwarenessFence locationFence = LocationFence.Exiting(location.Latitude, location.Longitude, _locationChangeRadiusMeters);
				UpdateRequestBuilder(locationFence, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Add, ref locationFenceRequestBuilder);
				await UpdateFencesAsync(locationFenceRequestBuilder.Build());
			}
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			// remove location fence
			FenceUpdateRequestBuilder locationFenceRequestBuilder = new FenceUpdateRequestBuilder();
			UpdateRequestBuilder(null, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Remove, ref locationFenceRequestBuilder);
			await UpdateFencesAsync(locationFenceRequestBuilder.Build());
		}
	}
}

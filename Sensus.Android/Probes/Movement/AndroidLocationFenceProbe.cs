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
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;
using Plugin.Permissions;
using Android.Gms.Awareness.Snapshot;
using Android.Gms.Awareness;
using Android.Gms.Awareness.Fence;

namespace Sensus.Android.Probes.Movement
{
    /// <summary>
    /// Provides location fence information via the Google Awareness API as <see cref="LocationDatum"/> readings.
    /// </summary>
    public class AndroidLocationFenceProbe : AndroidAwarenessProbe
    {
        public const string AWARENESS_EXITING_LOCATION_FENCE_KEY = "EXITING_LOCATION_FENCE";

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

            AwarenessBroadcastReceiver.LocationChanged += (sender, e) =>
            {
                RequestLocationSnapshotAsync();
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (AwarenessApiClient.IsConnected)
            {
                // we need location permission in order to snapshot / fence the user's location.
                if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
                {
                    string error = "Geolocation is not permitted on this device. Cannot start location fences.";
                    SensusServiceHelper.Get().FlashNotificationAsync(error);
                }
            }
            else
            {
                throw new Exception("Failed to connect with Google Awareness API.");
            }
        }

        protected override void StartListening()
        {
            base.StartListening();

            // request a location to start location fencing
            RequestLocationSnapshotAsync();
        }

        private void RequestLocationSnapshotAsync()
        {
            Task.Run(async () =>
            {
                if (await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location) == PermissionStatus.Granted)
                {
                    // store current location
                    ILocationResult locationResult = await Awareness.SnapshotApi.GetLocationAsync(AwarenessApiClient);
                    global::Android.Locations.Location location = locationResult.Location;
                    DateTimeOffset timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(location.Time);
                    await StoreDatumAsync(new LocationDatum(timestamp, location.HasAccuracy ? location.Accuracy : -1, location.Latitude, location.Longitude));

                    // replace the previous location fence with one around the current location. additions and removals are handled
                    // in the order specified below.
                    FenceUpdateRequestBuilder locationFenceRequestBuilder = new FenceUpdateRequestBuilder();
                    UpdateRequestBuilder(null, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Remove, ref locationFenceRequestBuilder);
                    AwarenessFence locationFence = LocationFence.Exiting(location.Latitude, location.Longitude, _locationChangeRadiusMeters);
                    UpdateRequestBuilder(locationFence, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Add, ref locationFenceRequestBuilder);
                    UpdateFences(locationFenceRequestBuilder.Build());
                }
            });
        }

        protected override void StopListening()
        {
            // remove location fence
            FenceUpdateRequestBuilder locationFenceRequestBuilder = new FenceUpdateRequestBuilder();
            UpdateRequestBuilder(null, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Remove, ref locationFenceRequestBuilder);
            UpdateFences(locationFenceRequestBuilder.Build());

            base.StopListening();
        }
    }
}

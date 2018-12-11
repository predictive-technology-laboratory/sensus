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

            AwarenessBroadcastReceiver.LocationChanged += async (sender, e) =>
            {
                await RequestLocationSnapshotAsync();
            };
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (AwarenessApiClient.IsConnected)
            {
                // we need location permission in order to snapshot / fence the user's location.
                if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
                {
                    string error = "Geolocation is not permitted on this device. Cannot start location fences.";
                    await SensusServiceHelper.Get().FlashNotificationAsync(error);
                }
            }
            else
            {
                throw new Exception("Failed to connect with Google Awareness API.");
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
                await UpdateFencesAsync(locationFenceRequestBuilder.Build());
            }
        }

        protected override async Task StopListeningAsync()
        {
            // remove location fence
            FenceUpdateRequestBuilder locationFenceRequestBuilder = new FenceUpdateRequestBuilder();
            UpdateRequestBuilder(null, AWARENESS_EXITING_LOCATION_FENCE_KEY, FenceUpdateAction.Remove, ref locationFenceRequestBuilder);
            await UpdateFencesAsync(locationFenceRequestBuilder.Build());

            await base.StopListeningAsync();
        }
    }
}

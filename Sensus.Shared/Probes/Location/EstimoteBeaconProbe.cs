﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Sensus.UI.UiProperties;
using System.Collections.Generic;
using Syncfusion.SfChart.XForms;
using Newtonsoft.Json;
using Plugin.Permissions.Abstractions;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using Sensus.Concurrent;
using System.Threading.Tasks;
using Sensus.Extensions;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Sensus uses [Estimote Beacons](http://estimote.com/#get-beacons) to track fine-grained locations indoors. This 
    /// Probe is available for Android and iOS and runs while the Sensus app is in the foreground and background. This Probe 
    /// generates proximity event data in the form of <see cref="EstimoteBeaconDatum"/> (for proximity events) and 
    /// <see cref="EstimoteIndoorLocationDatum"/> (for indoor x-y positioning readings.
    /// 
    /// # Prerequisites
    /// 
    ///   * Purchase Proximity or Location beacons.
    ///   * The beacons must be configured within the [Estimote Cloud console](https://cloud.estimote.com). To generate 
    ///     proximity (<see cref="EstimoteBeaconDatum"/>) readings, the beacons must have tags attached to them.
    ///     ```
    /// 
    /// More details are available [here](http://developer.estimote.com/proximity/android-tutorial).
    /// 
    /// * Having entered the App Id and App Token, the list of beacons and locations can be edited via the `Edit Beacons`
    ///   and `Edit Locations` buttons in the Estimote Beacon Probe configuration. 
    /// 
    /// ## Beacons 
    /// Each beacon definition contains the following:
    ///   
    ///   * `Beacon Tag`:  Tag of the beacon to detect, as specified above.
    ///   * `Proximity (Meters)`:  Number of meters desired for proximity.
    ///   * `Event Name`:  Name to be given to the proximity event.
    /// 
    /// ## Locations
    /// Each location contains a name and identifier.
    /// </summary>
    public abstract class EstimoteBeaconProbe : ListeningProbe
    {
        private ConcurrentObservableCollection<EstimoteBeacon> _beacons;
        private EstimoteLocation _location;

        public ConcurrentObservableCollection<EstimoteBeacon> Beacons
        {
            get
            {
                return _beacons;
            }
        }

        public EstimoteLocation Location
        {
            get
            {
                return _location;
            }
            set
            {
                _location = value;
            }
        }

        /// <summary>
        /// The App Id from the [Estimote Cloud console](https://cloud.estimote.com/#/apps) that is associated with the beacons to be tracked.
        /// </summary>
        /// <value>The Estimote Cloud app identifier.</value>
        [EntryStringUiProperty("Estimote Cloud App Id:", true, 35, true)]
        public string EstimoteCloudAppId { get; set; }

        /// <summary>
        /// The App Token from the [Estimote Cloud console](https://cloud.estimote.com/#/apps) that is associated with the beacons to be tracked.
        /// </summary>
        /// <value>The Estimote Cloud app token.</value>
        [EntryStringUiProperty("Estimote Cloud App Token:", true, 36, true)]
        public string EstimoteCloudAppToken { get; set; }

        /// <summary>
        /// Gets or sets the indoor location update interval. This is currently known to impact Android.
        /// </summary>
        /// <value>The indoor location update interval, in milliseconds.</value>
        [EntryIntegerUiProperty("Indoor Location Update Interval (MS)", true, 37, true)]
        public int IndoorLocationUpdateIntervalMS { get; set; } = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
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

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get
            {
                return "Estimote Beacons";
            }
        }

        [JsonIgnore]
        public override Type DatumType
        {
            get
            {
                return typeof(EstimoteBeaconDatum);
            }
        }

        public EstimoteBeaconProbe()
        {
            _beacons = new ConcurrentObservableCollection<EstimoteBeacon>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (string.IsNullOrWhiteSpace(EstimoteCloudAppId) || string.IsNullOrEmpty(EstimoteCloudAppToken))
            {
                throw new Exception("Must provide Estimote Cloud application ID and token.");
            }

            if (_beacons.Count == 0 && _location == null)
            {
                throw new Exception("Must add beacon(s) or a location.");
            }

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Estimote probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

            if (!await SensusServiceHelper.Get().EnableBluetoothAsync(true, "Sensus uses Bluetooth to identify beacons, which are being used in one of your studies."))
            {
                throw new Exception("Bluetooth not enabled.");
            }
        }

        public async Task<List<string>> GetBeaconTagsFromCloudAsync(TimeSpan? timeout = null)
        {
            List<string> tags = new List<string>();

            try
            {
                WebRequest request = WebRequest.Create("https://cloud.estimote.com/v3/devices");
                request.ContentType = "application/json";
                request.Method = "GET";
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(EstimoteCloudAppId + ":" + EstimoteCloudAppToken)));

                using (HttpWebResponse response = await request.GetResponseAsync(timeout) as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("Received non-OK status code:  " + response.StatusCode);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string content = reader.ReadToEnd();

                        if (string.IsNullOrWhiteSpace(content))
                        {
                            throw new Exception("Received empty response content.");
                        }

                        foreach (JObject device in JObject.Parse(content).Value<JArray>("data"))
                        {
                            try
                            {
                                foreach (string tag in device.Value<JObject>("shadow").Value<JArray>("tags"))
                                {
                                    if (!tags.Contains(tag))
                                    {
                                        tags.Add(tag);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to get Estimote beacons from Cloud:  " + ex, LoggingLevel.Normal, GetType());
                throw ex;
            }

            return tags;
        }

        public async Task<List<EstimoteLocation>> GetLocationsFromCloudAsync(TimeSpan? timeout)
        {
            List<EstimoteLocation> locations = new List<EstimoteLocation>();

            try
            {
                WebRequest request = WebRequest.Create("https://cloud.estimote.com/v1/indoor/locations");
                request.ContentType = "application/json";
                request.Method = "GET";
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(EstimoteCloudAppId + ":" + EstimoteCloudAppToken)));

                using (HttpWebResponse response = await request.GetResponseAsync(timeout) as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("Received non-OK status code:  " + response.StatusCode);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string content = reader.ReadToEnd();

                        if (string.IsNullOrWhiteSpace(content))
                        {
                            throw new Exception("Received empty response content.");
                        }

                        foreach (JObject location in JArray.Parse(content))
                        {
                            try
                            {
                                locations.Add(new EstimoteLocation(location.Value<string>("name"), location.Value<string>("identifier")));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to get Estimote locations from Cloud:  " + ex, LoggingLevel.Normal, GetType());
                throw ex;
            }

            return locations;
        }

        protected override ChartSeries GetChartSeries()
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

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }
    }
}
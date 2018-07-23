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

namespace Sensus.Probes.Location
{
    /// <summary>
    /// 
    /// Sensus uses [Estimote Proximity Beacons](http://estimote.com/#get-beacons) to track fine-grained locations. This Probe is available for 
    /// Android and iOS and runs while the Sensus app is in the foreground and background. Generates proximity event data in the form 
    /// of <see cref="EstimoteBeaconDatum"/> readings.
    /// 
    /// # Prerequisites
    /// 
    ///   * You must purchase Proximity or Location beacons.
    ///   * The beacons must be configured within the [Estimote Cloud console](https://cloud.estimote.com) to have the following JSON attachment:
    /// 
    ///     ```
    ///     { &quot;attachment&quot;: { &quot;sensus&quot;: &quot;test&quot; } }
    ///     ```
    /// 
    /// More details are available [here](http://developer.estimote.com/proximity/android-tutorial).
    /// 
    /// * Having entered the App Id and App Token, the list of beacons can be edited via the `Edit Beacons` button in the Estimote Beacon Probe 
    ///   configuration. Each beacon definition contains the following:
    ///   
    ///   * `Beacon Name`:  Name of the beacon, as specified in the attachment value.
    ///   * `Proximity (Meters)`:  Number of meters desired for proximity.
    ///   * `Event Name`:  Name to be given to the proximity event.
    /// 
    /// </summary>
    public abstract class EstimoteBeaconProbe : ListeningProbe
    {
        private ConcurrentObservableCollection<EstimoteBeacon> _beacons;

        public ConcurrentObservableCollection<EstimoteBeacon> Beacons { get { return _beacons; }}

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

        protected override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrWhiteSpace(EstimoteCloudAppId) || string.IsNullOrEmpty(EstimoteCloudAppToken))
            {
                throw new Exception("Must provide Estimote Cloud application ID and token.");
            }

            if (_beacons.Count == 0)
            {
                throw new Exception("Must add beacons.");
            }

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Estimote probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to identify beacons, which are being used in one of your studies."))
            {
                throw new Exception("Bluetooth not enabled.");
            }
        }

        public List<string> GetSensusBeaconNamesFromCloud()
        {
            List<string> sensusBeaconNames = new List<string>();

            try
            {
                WebRequest request = WebRequest.Create("https://cloud.estimote.com/v3/attachments");
                request.ContentType = "application/json";
                request.Method = "GET";
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(EstimoteCloudAppId + ":" + EstimoteCloudAppToken)));

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
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

                        foreach (JObject attachment in JObject.Parse(content).Value<JArray>("data"))
                        {
                            try
                            {
                                string deviceName = attachment.Value<JObject>("payload").Value<JValue>("sensus").ToString();

                                if (!sensusBeaconNames.Contains(deviceName))
                                {
                                    sensusBeaconNames.Add(deviceName);
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

            return sensusBeaconNames;
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return null;
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
        }
    }
}

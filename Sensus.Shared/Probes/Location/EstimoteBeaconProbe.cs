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
    public abstract class EstimoteBeaconProbe : ListeningProbe
    {
        private ConcurrentObservableCollection<EstimoteBeacon> _beacons;

        public ConcurrentObservableCollection<EstimoteBeacon> Beacons { get { return _beacons; }}

        [EntryStringUiProperty("Estimote Cloud App Id:", true, 35)]
        public string EstimoteCloudAppId { get; set; }

        [EntryStringUiProperty("Estimote Cloud App Token:", true, 36)]
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
                WebRequest request = WebRequest.Create("https://cloud.estimote.com/v2/devices");
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

                        foreach (JObject device in JArray.Parse(content))
                        {
                            JArray tags = device.Value<JObject>("shadow").Value<JArray>("tags");
                            foreach (JValue tag in tags)
                            {
                                try
                                {
                                    // there might be other tags on the beacon. skip those that aren't objects by catching exception.
                                    JObject tagObject = JObject.Parse(tag.ToString());

                                    // there might be other objects. catch exception to skip those that aren't attachments.
                                    string deviceName = tagObject.Value<JObject>("attachment").Value<JValue>("sensus").ToString();

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
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to get Estimote beacons from Cloud:  " + ex, LoggingLevel.Normal, GetType());
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

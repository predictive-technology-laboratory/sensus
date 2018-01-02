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
using System.Linq;
using Newtonsoft.Json;
using Plugin.Permissions.Abstractions;
using EstimoteSdk.Observation.Region;
using EstimoteSdk.Common.Config;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;

#if __ANDROID__
using Android.App;
#endif

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconProbe : ListeningProbe
    {
        private EstimoteBeaconManager _beaconManager;
        private List<EstimoteBeacon> _beacons;

        [EditorUiProperty("Beacons (One Per Line):", true, 30)]
        public string Beacons
        {
            get
            {
                return string.Join(Environment.NewLine, _beacons.Select(beacon => beacon.ToString()));
            }
            set
            {
                _beacons = value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(beaconString => EstimoteBeacon.FromString(beaconString)).Where(beacon => beacon != null).ToList();
            }
        }

        [EntryIntegerUiProperty("Foreground Scan Period (Seconds):", true, 31)]
        public int ForegroundScanPeriodSeconds { get; set; }

        [EntryIntegerUiProperty("Foreground Wait Period (Seconds):", true, 32)]
        public int ForegroundWaitPeriodSeconds { get; set; }

        [EntryIntegerUiProperty("Background Scan Period (Seconds):", true, 33)]
        public int BackgroundScanPeriodSeconds { get; set; }

        [EntryIntegerUiProperty("Background Wait Period (Seconds):", true, 34)]
        public int BackgroundWaitPeriodSeconds { get; set; }

        [EntryStringUiProperty("Estimote Cloud App Id:", true, 35)]
        public string EstimoteCloudAppId { get; set; }

        [EntryStringUiProperty("Estimote Cloud App Token:", true, 36)]
        public string EstimoteCloudAppToken { get; set; }

        [EditableListUiProperty("Beacon Tags (One Regex Per Line):", true, 37)]
        public List<string> BeaconTags { get; set; }

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
            _beaconManager = new EstimoteBeaconManager();
            _beacons = new List<EstimoteBeacon>();

            ForegroundScanPeriodSeconds = 10;
            ForegroundWaitPeriodSeconds = 30;
            BackgroundScanPeriodSeconds = 10;
            BackgroundWaitPeriodSeconds = 30;

            _beaconManager.LocationFound += async (sender, location) =>
            {
                foreach (EstimoteBeacon beacon in _beacons)
                {
                    if (beacon.Identifier == location.Id.ToString().Trim('[', ']') && beacon.ProximityConditionSatisfiedBy(RegionUtils.ComputeProximity(location)))
                    {
                        await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon.Identifier, "EnteredProximity"));
                    }
                }
            };

            _beaconManager.TelemetryReceived += async (sender, telemetry) =>
            {
                string beaconIdentifier = telemetry.DeviceId.ToString().Trim('[', ']');

                // check if the telemetry is from a beacon we are scanning for
                if (!_beacons.Any(beacon => beacon.Identifier == beaconIdentifier))
                {
                    return;
                }

                DateTimeOffset telemetryTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(telemetry.Timestamp.Time);

                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.Accelerometer) + ":" + telemetry.Accelerometer));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.AmbientLight) + ":" + telemetry.AmbientLight));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.Magnetometer) + ":" + telemetry.Magnetometer));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.MotionDuration) + ":" + telemetry.MotionDuration));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.MotionState) + ":" + telemetry.MotionState));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.Pressure) + ":" + telemetry.Pressure));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.PreviousMotionDuration) + ":" + telemetry.PreviousMotionDuration));
                await StoreDatumAsync(new EstimoteBeaconDatum(telemetryTimestamp, beaconIdentifier, nameof(telemetry.Temperature) + ":" + telemetry.Temperature));
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start Estimote probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

            if (!string.IsNullOrWhiteSpace(EstimoteCloudAppId) && !string.IsNullOrEmpty(EstimoteCloudAppToken))
            {
                try
                {
#if __ANDROID__
                    Estimote.Initialize(Application.Context, EstimoteCloudAppId, EstimoteCloudAppToken);
#elif __IOS__
                    asdf
#endif

                    if (BeaconTags.Count > 0)
                    {
                        GetBeaconsFromCloud();
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while initializing Estimote Cloud:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        private void GetBeaconsFromCloud()
        {
            WebRequest request = WebRequest.Create("https://cloud.estimote.com/v2/devices");
            request.ContentType = "application/json";
            request.Method = "GET";
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(EstimoteCloudAppId + ":" + EstimoteCloudAppToken)));

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string content = reader.ReadToEnd();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return;
                    }

                    List<Regex> tagREs = BeaconTags.Select(tag => new Regex(tag)).ToList();

                    foreach (JObject device in JArray.Parse(content))
                    {
                        try
                        {
                            string identifier = device.GetValue("identifier").ToString();
                            string[] sensusTag = device.Value<JObject>("shadow").Value<JArray>("tags").Select(tag => tag.ToString()).Where(tag => tag.StartsWith("sensus")).Select(tag => tag.Split(':')).FirstOrDefault();
                            if (sensusTag != null)
                            {
                                if (tagREs.Any(tagRE => tagRE.IsMatch(sensusTag[1])))
                                {
                                    EstimoteBeacon beacon = EstimoteBeacon.FromString(identifier + ":" + sensusTag[2]);

                                    if (!_beacons.Contains(beacon))
                                    {
                                        _beacons.Add(beacon);
                                    }
                                }
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }
                }
            }
        }

        protected override void StartListening()
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to identify beacons, which are being used in one of your studies."))
            {
                throw new Exception("Bluetooth not enabled.");
            }

            _beaconManager.ConnectAndStartScanning(TimeSpan.FromSeconds(ForegroundScanPeriodSeconds), TimeSpan.FromSeconds(ForegroundWaitPeriodSeconds), TimeSpan.FromSeconds(BackgroundScanPeriodSeconds), TimeSpan.FromSeconds(BackgroundWaitPeriodSeconds));
        }

        protected override void StopListening()
        {
            _beaconManager.Disconnect();
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

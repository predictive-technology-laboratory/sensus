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

namespace Sensus.Probes.Location
{
    /// <summary>
    /// 
    /// Sensus uses [Estimote Proximity Beacons](http://estimote.com/#get-beacons) to track fine-grained locations indoors. This 
    /// Probe is available for Android and iOS and runs while the Sensus app is in the foreground and background. This Probe 
    /// generates proximity event data in the form of <see cref="EstimoteBeaconDatum"/> readings.
    /// 
    /// # Prerequisites
    /// 
    ///   * Purchase Proximity or Location beacons.
    ///   * The beacons must be configured within the [Estimote Cloud console](https://cloud.estimote.com) and have tags attached to them.
    ///     ```
    /// 
    /// More details are available [here](http://developer.estimote.com/proximity/android-tutorial).
    /// 
    /// * Having entered the App Id and App Token, the list of beacons can be edited via the `Edit Beacons` button in the Estimote Beacon Probe 
    ///   configuration. Each beacon definition contains the following:
    ///   
    ///   * `Beacon Tag`:  Tag of the beacon to detect, as specified above.
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

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (string.IsNullOrWhiteSpace(EstimoteCloudAppId) || string.IsNullOrEmpty(EstimoteCloudAppToken))
            {
                throw new Exception("Must provide Estimote Cloud application ID and token.");
            }

            if (_beacons.Count == 0)
            {
                throw new Exception("Must add beacons.");
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

        public List<string> GetBeaconTagsFromCloud()
        {
            List<string> tags = new List<string>();

            try
            {
                WebRequest request = WebRequest.Create("https://cloud.estimote.com/v3/devices");
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

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

            _beaconManager.EnteredRegion += async (sender, region) =>
            {
                await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, region, true));
            };

            _beaconManager.ExitedRegion += async (sender, region) =>
            {
                await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, region, false));
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
        }

        protected override void StartListening()
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to identify beacons, which are being used in one of your studies."))
            {
                throw new Exception("Bluetooth not enabled.");
            }

            _beaconManager.Connect(_beacons, TimeSpan.FromSeconds(ForegroundScanPeriodSeconds), TimeSpan.FromSeconds(ForegroundWaitPeriodSeconds), TimeSpan.FromSeconds(BackgroundScanPeriodSeconds), TimeSpan.FromSeconds(BackgroundWaitPeriodSeconds));
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

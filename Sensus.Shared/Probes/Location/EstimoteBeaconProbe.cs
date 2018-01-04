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
using Sensus.Context;

#if __ANDROID__
using Estimote.Android.Proximity;
using Estimote.Android.Cloud;
using Android.App;
using Sensus.Android;
#elif __IOS__
using Estimote;
#endif

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconProbe : ListeningProbe
    {
        private class EnterProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                return null;
            }
        }

        private class ExitProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                return null;
            }
        }

        private class ErrorHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                return null;
            }
        }

        private List<EstimoteBeacon> _beacons;
        IProximityObserver _proximityObserver;
        IProximityObserverHandler _proximityObservationHandler;

        [EditorUiProperty("Beacons (One \"Identifier:Meters\" Per Line):", true, 30)]
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
            _beacons = new List<EstimoteBeacon>();
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

            if (string.IsNullOrWhiteSpace(EstimoteCloudAppId) || string.IsNullOrEmpty(EstimoteCloudAppToken))
            {
                throw new Exception("Must provide Estimote Cloud application ID and token.");
            }
        }

        protected override void StartListening()
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to identify beacons, which are being used in one of your studies."))
            {
                throw new Exception("Bluetooth not enabled.");
            }

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _proximityObserver = new ProximityObserverFactory().Create(Application.Context, new EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken));

                List<IProximityZone> zones = new List<IProximityZone>();

                foreach (EstimoteBeacon beacon in _beacons)
                {
                    IProximityZone zone = _proximityObserver
                        .ZoneBuilder()
                        .ForAttachmentKeyAndValue("sensus", beacon.Identifier)
                        .InCustomRange(beacon.ProximityMeters)
                        .WithOnEnterAction(new EnterProximityHandler())
                        .WithOnExitAction(new ExitProximityHandler())
                        .Create();

                    zones.Add(zone);
                }

                Notification notification = new Notification.Builder(Application.Context)
                                                            //.SetSmallIcon(Resource.Drawable.ic_launcher)
                                                            .SetContentTitle("Beacon scan")
                                                            .SetContentText("Scan is running...")
                                                            .Build();

                _proximityObservationHandler = _proximityObserver
                    .AddProximityZones(zones.ToArray())
                    .WithBalancedPowerMode()
                    .WithOnErrorAction(new ErrorHandler())
                    .StartWithScannerInForegroundService(notification);
            });
        }

        protected override void StopListening()
        {
            _proximityObservationHandler.Stop();
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

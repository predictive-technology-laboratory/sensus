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
using System.Collections.Generic;
using Sensus.Context;
using System.Linq;

#if __ANDROID__
using Android.App;
using EstimoteSdk;
#elif __IOS__
using Estimote;
using Region = CoreLocation.CLBeaconRegion;
#endif

namespace Sensus.Probes.Location
{
#if __ANDROID__
    public class EstimoteBeaconManager : Java.Lang.Object, BeaconManager.IServiceReadyCallback, BeaconManager.IMonitoringListener
#elif __IOS__
    public class EstimoteBeaconManager
#endif
    {
        public static bool RegionsAreEqual(Region region1, Region region2)
        {
            return region1.Identifier == region2.Identifier &&
#if __ANDROID__
                   region1.ProximityUUID.ToString() == region2.ProximityUUID.ToString() &&
#elif __IOS__
                   region1.ProximityUuid.ToString() == region2.ProximityUuid.ToString() &&
#endif
                   region1.Major == region2.Major &&
                   region1.Minor == region2.Minor;
        }

        public event EventHandler<Region> EnteredRegion;
        public event EventHandler<Region> ExitedRegion;

        private BeaconManager _beaconManager;
        private List<EstimoteBeacon> _beacons;
        private DeviceManager _deviceManager;

        public void Connect(List<EstimoteBeacon> beacons, TimeSpan foregroundScanPeriod, TimeSpan foregroundWaitTime, TimeSpan backgroundScanPeriod, TimeSpan backgroundWaitTime)
        {
            _beacons = beacons;

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
#if __ANDROID__
                _beaconManager = new BeaconManager(Application.Context);
                _beaconManager.SetMonitoringListener(this);
                _beaconManager.SetForegroundScanPeriod((long)foregroundScanPeriod.TotalMilliseconds, (long)foregroundWaitTime.TotalMilliseconds);
                _beaconManager.SetBackgroundScanPeriod((long)backgroundScanPeriod.TotalMilliseconds, (long)backgroundWaitTime.TotalMilliseconds);
                _beaconManager.Connect(this);
#elif __IOS__
                _beaconManager = new BeaconManager();

                _beaconManager.EnteredRegion += (sender, e) =>
                {
                    if (_beacons.Any(beacon => RegionsAreEqual(beacon.Region, e.Region)))
                    {
                        EnteredRegion?.Invoke(this, e.Region);
                    }
                };

                _beaconManager.ExitedRegion += (sender, e) =>
                {
                    if (_beacons.Any(beacon => RegionsAreEqual(beacon.Region, e.Region)))
                    {
                        ExitedRegion?.Invoke(this, e.Region);
                    }
                };

                foreach (EstimoteBeacon beacon in _beacons)
                {
                    _beaconManager.StartMonitoringForRegion(beacon.Region);
                }

                _deviceManager = new DeviceManager();
                _deviceManager.
                _deviceManager.RegisterForTelemetryNotification(new EstimoteTelemetryMotion());
#endif
            });
        }

#if __ANDROID__
        public void OnServiceReady()
        {
            foreach (EstimoteBeacon beacon in _beacons)
            {
                _beaconManager.StartMonitoring(beacon.Region);
            }
        }

        public void OnEnteredRegion(Region region, IList<Beacon> beacons)
        {
            if (_beacons.Any(beacon => RegionsAreEqual(beacon.Region, region)))
            {
                EnteredRegion?.Invoke(this, region);
            }
        }

        public void OnExitedRegion(Region region)
        {
            if (_beacons.Any(beacon => RegionsAreEqual(beacon.Region, region)))
            {
                ExitedRegion?.Invoke(this, region);
            }
        }
#endif

        public void Disconnect()
        {
            foreach (EstimoteBeacon beacon in _beacons)
            {
                try
                {
#if __ANDROID__
                    _beaconManager.StopMonitoring(beacon.Region);
#elif __IOS__
                    _beaconManager.StopMonitoringForRegion(beacon.Region);
#endif
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Error stopping Estimote monitoring:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

#if __ANDROID__
            try
            {
                _beaconManager.Disconnect();
            }
            catch (Exception)
            {

            }
#endif

            try
            {
                _beaconManager.Dispose();
            }
            catch (Exception)
            {
            }

            _beaconManager = null;
        }
    }
}
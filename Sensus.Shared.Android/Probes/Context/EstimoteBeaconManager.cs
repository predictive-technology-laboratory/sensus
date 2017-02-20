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

using Android.App;
using EstimoteSdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconManager :  Java.Lang.Object, BeaconManager.IServiceReadyCallback, BeaconManager.IMonitoringListener
    {
        public static bool SameRegion(Region region1, Region region2)
        {
            return region1.Identifier == region2.Identifier && 
                   region1.ProximityUUID.ToString() == region2.ProximityUUID.ToString() && 
                   region1.Major == region2.Major && 
                   region1.Minor == region2.Minor;
        }

        public event EventHandler<Tuple<Region, IList<Beacon>>> EnteredRegion;
        public event EventHandler<Region> ExitedRegion;

        private BeaconManager _beaconManager;
        private List<EstimoteBeacon> _beacons;

        public EstimoteBeaconManager()
        {
            _beaconManager = new BeaconManager(Application.Context);
            _beaconManager.SetMonitoringListener(this);
        }

        public void SetForegroundScanPeriod(TimeSpan foregroundScanPeriod, TimeSpan foregroundWaitTime)
        {
            _beaconManager.SetForegroundScanPeriod((long)foregroundScanPeriod.TotalMilliseconds, (long)foregroundWaitTime.TotalMilliseconds);
        }

        public void SetBackgroundScanPeriod(TimeSpan backgroundScanPeriod, TimeSpan backgroundWaitTime)
        {
            _beaconManager.SetBackgroundScanPeriod((long)backgroundScanPeriod.TotalMilliseconds, (long)backgroundWaitTime.TotalMilliseconds);
        }

        public void Connect(List<EstimoteBeacon> beacons)
        {
            _beacons = beacons;
            _beaconManager.Connect(this);
        }

        public void OnServiceReady()
        {
            foreach (EstimoteBeacon beacon in _beacons)
            {
                _beaconManager.StartMonitoring(beacon.Region);
            }
        }

        public void OnEnteredRegion(Region region, IList<Beacon> beacons)
        {
            if (_beacons.Any(beacon => SameRegion(beacon.Region, region)))
            {
                EnteredRegion?.Invoke(this, new Tuple<Region, IList<Beacon>>(region, beacons));
            }
        }

        public void OnExitedRegion(Region region)
        {
            if (_beacons.Any(beacon => SameRegion(beacon.Region, region)))
            {
                ExitedRegion?.Invoke(this, region);
            }
        }

        public void Disconnect()
        {
            foreach (EstimoteBeacon beacon in _beacons)
            {
                try
                {
                    _beaconManager.StopMonitoring(beacon.Region);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Error stopping Estimote monitoring:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }            

            _beaconManager.Disconnect();
        }
    }
}
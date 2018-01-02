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

#if __ANDROID__
using Android.App;
using EstimoteSdk.Service;
using EstimoteSdk.Recognition.Packets;
#elif __IOS__
using Estimote;
#endif

namespace Sensus.Probes.Location
{
#if __ANDROID__
    public class EstimoteBeaconManager : Java.Lang.Object, BeaconManager.IServiceReadyCallback, BeaconManager.ILocationListener, BeaconManager.ITelemetryListener
#elif __IOS__
    public class EstimoteBeaconManager
#endif
    {
        public event EventHandler<EstimoteLocation> LocationFound;
        public event EventHandler<EstimoteTelemetry> TelemetryReceived;

#if __ANDROID__
        private BeaconManager _beaconManager;

        public void ConnectAndStartScanning(TimeSpan foregroundScanPeriod, TimeSpan foregroundWaitTime, TimeSpan backgroundScanPeriod, TimeSpan backgroundWaitTime)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _beaconManager = new BeaconManager(Application.Context);
                _beaconManager.SetLocationListener(this);
                _beaconManager.SetTelemetryListener(this);
                _beaconManager.SetForegroundScanPeriod((long)foregroundScanPeriod.TotalMilliseconds, (long)foregroundWaitTime.TotalMilliseconds);
                _beaconManager.SetBackgroundScanPeriod((long)backgroundScanPeriod.TotalMilliseconds, (long)backgroundWaitTime.TotalMilliseconds);
                _beaconManager.Connect(this);
            });
        }

        public void OnServiceReady()
        {
            _beaconManager.StartLocationDiscovery();
            _beaconManager.StartTelemetryDiscovery();
        }

        public void OnLocationsFound(IList<EstimoteLocation> locations)
        {
            foreach (EstimoteLocation location in locations)
            {
                LocationFound?.Invoke(this, location);
            }
        }

        public void OnTelemetriesFound(IList<EstimoteTelemetry> telemetries)
        {
            foreach (EstimoteTelemetry telemetry in telemetries)
            {
                TelemetryReceived?.Invoke(this, telemetry);
            }
        }
#elif __IOS__
        public void ConnectAndStartScanning()
        {
            throw new NotImplementedException();
        }
#endif

        public void Disconnect()
        {
            try
            {
#if __ANDROID__
                _beaconManager.StopLocationDiscovery();
                _beaconManager.StopTelemetryDiscovery();
#elif __IOS__
                throw new NotImplementedException();
#endif
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Error stopping Estimote monitoring:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

#if __ANDROID__
            try
            {
                _beaconManager.Disconnect();
            }
            catch (Exception)
            {

            }

            _beaconManager = null;
#elif __IOS__
            throw new NotImplementedException();
#endif
        }
    }
}
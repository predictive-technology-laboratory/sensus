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
using Sensus.Probes.Location;
using Estimote.iOS.Proximity;
using System.Collections.Generic;
using Sensus.Context;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Location
{
    public class iOSEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private ProximityObserver _observer;

        protected override Task StartListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                _observer = new ProximityObserver(new CloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken), error =>
                {
                    SensusServiceHelper.Get().Logger.Log("Error while initializing proximity observer:  " + error, LoggingLevel.Normal, GetType());
                });

                List<ProximityZone> zones = new List<ProximityZone>();

                foreach (EstimoteBeacon beacon in Beacons)
                {
                    ProximityZone zone = new ProximityZone(beacon.Tag, new ProximityRange(beacon.ProximityMeters))
                    {
                        OnEnter = async (triggeringDeviceAttachment) =>
                        {
                            await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Entered));
                        },

                        OnExit = async (triggeringDeviceAttachment) =>
                        {
                            await StoreDatumAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, beacon, EstimoteBeaconProximityEvent.Exited));
                        }
                    };

                    zones.Add(zone);
                }

                _observer.StartObservingZones(zones.ToArray());
            });

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(_observer.StopObservingZones);

            return Task.CompletedTask;
        }
    }
}

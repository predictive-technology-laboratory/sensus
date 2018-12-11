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
using Estimote.Android.Proximity;
using Android.App;
using Sensus.Context;
using System.Collections.Generic;
using Sensus.Probes.Location;
using Sensus.Android.Notifications;
using System.Threading.Tasks;

// the unit test project contains the Resource class in its namespace rather than the Sensus.Android
// namespace. include that namespace below.
#if UNIT_TEST
using Sensus.Android.Tests;
#endif

namespace Sensus.Android.Probes.Location
{
    public class AndroidEstimoteBeaconProbe : EstimoteBeaconProbe
    {
        private class ProximityHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public AndroidEstimoteBeaconProbe _probe;
            public EstimoteBeacon _beacon;
            public EstimoteBeaconProximityEvent _proximityEvent;

            public ProximityHandler(AndroidEstimoteBeaconProbe probe, EstimoteBeacon beacon, EstimoteBeaconProximityEvent proximityEvent)
            {
                _probe = probe;
                _beacon = beacon;
                _proximityEvent = proximityEvent;
            }

            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                InvokeAsync(new EstimoteBeaconDatum(DateTimeOffset.UtcNow, _beacon, _proximityEvent));
                return null;
            }

            /// <summary>
            /// Re-declared <see cref="Invoke(Java.Lang.Object)"/> to properly await result.
            /// </summary>
            /// <param name="datum">Datum.</param>
            private async void InvokeAsync(EstimoteBeaconDatum datum)
            {
                await _probe.StoreDatumAsync(datum);
            }
        }

        private class ErrorHandler : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            public Java.Lang.Object Invoke(Java.Lang.Object p0)
            {
                SensusServiceHelper.Get().Logger.Log("Error while observing Estimote beacons:  " + p0, LoggingLevel.Normal, GetType());
                return null;
            }
        }

        IProximityObserver _proximityObserver;
        IProximityObserverHandler _proximityObservationHandler;

        protected override Task StartListeningAsync()
        {
            Notification notification = (SensusContext.Current.Notifier as AndroidNotifier).CreateNotificationBuilder(Application.Context, AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                                                           .SetSmallIcon(Resource.Drawable.notification_icon_background)
                                                                                           .SetContentTitle("Beacon Scan")
                                                                                           .SetContentText("Scanning...")
                                                                                           .SetOngoing(true)
                                                                                           .Build();

            _proximityObserver = new ProximityObserverBuilder(Application.Context, new EstimoteCloudCredentials(EstimoteCloudAppId, EstimoteCloudAppToken))
                .WithBalancedPowerMode()
                .WithScannerInForegroundService(notification)
                .OnError(new ErrorHandler())
                .Build();

            List<IProximityZone> zones = new List<IProximityZone>();

            foreach (EstimoteBeacon beacon in Beacons)
            {
                IProximityZone zone = new ProximityZoneBuilder()
                                              .ForTag(beacon.Tag)
                                              .InCustomRange(beacon.ProximityMeters)
                                              .OnEnter(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Entered))
                                              .OnExit(new ProximityHandler(this, beacon, EstimoteBeaconProximityEvent.Exited))
                                              .Build();

                zones.Add(zone);
            }

            _proximityObservationHandler = _proximityObserver.StartObserving(zones);

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _proximityObservationHandler.Stop();

            return Task.CompletedTask;
        }
    }
}

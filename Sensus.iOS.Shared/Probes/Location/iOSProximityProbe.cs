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

using Foundation;
using Plugin.Permissions.Abstractions;
using Sensus.Exceptions;
using Sensus.Probes.Location;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Location
{
    /// <summary>
    /// iOS proximity probe. Will report distance from phone to a nearby object. Readings from this sensor
    /// will not be reported when the app is in the background.
    /// </summary>
    public class iOSProximityProbe : ProximityProbe
    {       
        private NSObject _notification;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Sensors) == PermissionStatus.Granted)
            {
                // enable proximity monitoring, is set to false by default, and if the device cannot monitor proximity, then it will remain false.
                UIDevice.CurrentDevice.ProximityMonitoringEnabled = true;
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "This device does not have a proximity sensor, or the user has denied access to it. Cannot start proximity probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override Task StartListeningAsync()
        {
            // Check to see if Proximity enabled is true, this would mean that the device can monitor proximity
            if (UIDevice.CurrentDevice.ProximityMonitoringEnabled)
            {
                // Hook the sensor event 
                // https://developer.xamarin.com/api/member/MonoTouch.UIKit.UIDevice+Notifications.ObserveProximityStateDidChange/p/System.EventHandler%7BMonoTouch.Foundation.NSNotificationEventArgs%7D/
                _notification = UIDevice.Notifications.ObserveProximityStateDidChange(async (o, e) =>
                {
                    try
                    {
                        // apple has a proximitystate bool that returns 1 if device is close to user and 0 if it is not
                        await StoreDatumAsync(new ProximityDatum(DateTimeOffset.UtcNow, (UIDevice.CurrentDevice.ProximityState ? 0 : 1), 1));
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Exception while storing datum:  " + ex.Message, ex);
                    }
                });
            }

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            // disable proximity monitoring, as it might be slightly irritating to the user for the screen to go blank.
            UIDevice.CurrentDevice.ProximityMonitoringEnabled = false;

            _notification.Dispose();

            return Task.CompletedTask;
        }
    }
}
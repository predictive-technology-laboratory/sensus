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


using CoreMotion;
using Foundation;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Location;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.iOS.Probes.Location
{
    public class iOSAltitudeProbe : AltitudeProbe
    {
        private CMAltimeter _altitudeChangeListener;

        protected override void Initialize()
        {
            base.Initialize();

            //the IsRelativeAltitudeAvailable is used to check to see if the device has altitude capablities based on
            //documentation here https://developer.apple.com/documentation/coremotion/cmaltimeter

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Sensors) == PermissionStatus.Granted && CMAltimeter.IsRelativeAltitudeAvailable)
            {
                _altitudeChangeListener = new CMAltimeter();
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "This device does not contain an accelerometer, or the user has denied access to it. Cannot start accelerometer probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void StartListening()
        {
            _altitudeChangeListener?.StartRelativeAltitudeUpdates(new NSOperationQueue(), (data, error) =>
            {
                if (data != null && error == null)
                {
                    //iOS reports kilopascals, in order to share Altitude constructor with 
                    //Android, convert kPa to hPa (kPa * 10) 
                    //https://www.unitjuggler.com/convert-pressure-from-hPa-to-kPa.html?val=10

                    double hPa = data.Pressure.DoubleValue * 10;

                    StoreDatum(new AltitudeDatum(DateTimeOffset.UtcNow, hPa));
                }
            });
        }

        protected override void StopListening()
        {
            _altitudeChangeListener.StopRelativeAltitudeUpdates();
        }
    }
}

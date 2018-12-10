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

using CoreMotion;
using Foundation;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Location;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Location
{
    public class iOSAltitudeProbe : AltitudeProbe
    {
        private CMAltimeter _altitudeChangeListener;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            //the IsRelativeAltitudeAvailable is used to check to see if the device has altitude capablities based on
            //documentation here https://developer.apple.com/documentation/coremotion/cmaltimeter

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Sensors) == PermissionStatus.Granted && CMAltimeter.IsRelativeAltitudeAvailable)
            {
                _altitudeChangeListener = new CMAltimeter();
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "This device does not contain an altimeter, or the user has denied access to it. Cannot start altitude probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override Task StartListeningAsync()
        {
            _altitudeChangeListener?.StartRelativeAltitudeUpdates(new NSOperationQueue(), async (data, error) =>
            {
                if (data?.Pressure != null && error == null)
                {
                    // iOS reports kilopascals, in order to share Altitude constructor with 
                    // Android, convert kPa to hPa (kPa * 10) 
                    // https://www.unitjuggler.com/convert-pressure-from-hPa-to-kPa.html?val=10
                    await StoreDatumAsync(new AltitudeDatum(DateTimeOffset.UtcNow, data.Pressure.DoubleValue * 10));
                }
            });

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _altitudeChangeListener?.StopRelativeAltitudeUpdates();

            return Task.CompletedTask;
        }
    }
}

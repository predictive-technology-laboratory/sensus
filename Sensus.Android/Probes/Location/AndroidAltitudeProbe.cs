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

using Android.Hardware;
using SensusService.Probes.Location;
using System;
using Newtonsoft.Json;

namespace Sensus.Android.Probes.Location
{
    public class AndroidAltitudeProbe : AltitudeProbe
    {
        private AndroidSensorListener _altitudeListener;

        /// <summary>
        /// If true, all updates will be received. If false, updates will only be dependably received when the device is awake.
        /// </summary>
        /// <value>True.</value>
        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return true;
            }
        }

        public AndroidAltitudeProbe()
        {
            _altitudeListener = new AndroidSensorListener(SensorType.Pressure, SensorDelay.Normal, null, e =>
                {
                    // http://www.srh.noaa.gov/images/epz/wxcalc/pressureAltitude.pdf
                    double hPa = e.Values[0];
                    double stdPressure = 1013.25;
                    double altitude = (1 - Math.Pow((hPa / stdPressure), 0.190284)) * 145366.45;

                    StoreDatum(new AltitudeDatum(DateTimeOffset.UtcNow, -1, altitude));
                });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _altitudeListener.Initialize();
        }

        protected override void StartListening()
        {
            _altitudeListener.Start();
        }

        protected override void StopListening()
        {
            _altitudeListener.Stop();
        }
    }
}

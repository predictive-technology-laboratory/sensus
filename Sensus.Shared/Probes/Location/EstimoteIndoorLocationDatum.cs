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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#if __ANDROID__
using EstimoteIndoorPosition = Estimote.Android.Indoor.LocationPosition;
using EstimoteIndoorLocation = Estimote.Android.Indoor.Location;
#elif __IOS__
using EstimoteIndoorLocation = Estimote.iOS.Indoor.EILLocation;
using EstimoteIndoorPosition = Estimote.iOS.Indoor.EILOrientedPoint;
#endif

namespace Sensus.Probes.Location
{
    /// <summary>
    /// The location of the device within a space monitored by Estimote indoor location beacons. See <see cref="EstimoteBeaconProbe"/>
    /// for more information.
    /// </summary>
    public class EstimoteIndoorLocationDatum : Datum
    {
        private double _x;
        private double _y;
        private double _orientation;
        private EstimoteIndoorLocationAccuracy _accuracy;
        private string _locationName;
        private string _locationId;
        private EstimoteIndoorLocation _estimoteLocation;
        private EstimoteIndoorPosition _estimotePosition;

        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public double Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public EstimoteIndoorLocationAccuracy Accuracy
        {
            get { return _accuracy; }
            set { _accuracy = value; }
        }

        public string LocationName
        {
            get { return _locationName; }
            set { _locationName = value; }
        }

        public string LocationId
        {
            get { return _locationId; }
            set { _locationId = value; }
        }

        /// <summary>
        /// A convenience reference to the native Estimote location object. Not serialized to the data store.
        /// </summary>
        /// <value>The Estimote position.</value>
        public EstimoteIndoorLocation EstimoteLocation
        {
            get { return _estimoteLocation; }
        }

        /// <summary>
        /// A convenience reference to the native Estimote position object. Not serialized to the data store.
        /// </summary>
        /// <value>The Estimote position.</value>
        [JsonIgnore]
        public EstimoteIndoorPosition EstimotePosition
        {
            get { return _estimotePosition; }
        }

        public override string DisplayDetail
        {
            get { return _locationName + " (X=" + Math.Round(_x, 1) + ", Y=" + Math.Round(_y, 1) + ", O=" + Math.Round(_orientation) + ")"; }
        }

        public override object StringPlaceholderValue
        {
            get { return _locationName; }
        }

        public EstimoteIndoorLocationDatum(DateTimeOffset timestamp, double x, double y, double orientation, EstimoteIndoorLocationAccuracy accuracy, string locationName, string locationId, EstimoteIndoorLocation estimoteLocation, EstimoteIndoorPosition estimotePosition)
            : base(timestamp)
        {
            _x = x;
            _y = y;
            _orientation = orientation;
            _accuracy = accuracy;
            _locationName = locationName;
            _locationId = locationId;
            _estimoteLocation = estimoteLocation;
            _estimotePosition = estimotePosition;
        }
    }
}
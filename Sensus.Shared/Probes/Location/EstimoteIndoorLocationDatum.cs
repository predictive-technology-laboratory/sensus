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
        private EstimoteIndoorLocationAccuracy _accuracy;
        private string _locationName;
        private string _locationId;

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

        public override string DisplayDetail
        {
            get { return _locationName + " (X=" + _x + ", Y=" + _y + ")"; }
        }

        public override object StringPlaceholderValue
        {
            get { return _locationName; }
        }

        public EstimoteIndoorLocationDatum(DateTimeOffset timestamp, double x, double y, EstimoteIndoorLocationAccuracy accuracy, string locationName, string locationId)
            : base(timestamp)
        {
            _x = x;
            _y = y;
            _accuracy = accuracy;
            _locationName = locationName;
            _locationId = locationId;
        }
    }
}
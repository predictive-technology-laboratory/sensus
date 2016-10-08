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
using SensusService.Probes.Movement;
using Plugin.Geolocator.Abstractions;

namespace SensusService.Probes.Location
{
    public class PointOfInterest
    {
        private string _name;
        private string _type;
        private Position _position;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        public Position Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private PointOfInterest() 
        {
        }

        public PointOfInterest(string name, string type, Position position)
            : this()
        {
            _name = name ?? "";
            _type = type ?? "";
            _position = position;
        }

        public bool Triggers(PointOfInterestProximityTrigger trigger, double distanceMeters)
        {
            return 
            (string.IsNullOrWhiteSpace(trigger.PointOfInterestName) || _name == trigger.PointOfInterestName) &&
            (string.IsNullOrWhiteSpace(trigger.PointOfInterestType) || _type == trigger.PointOfInterestType) &&
            (trigger.DistanceThresholdDirection == ProximityThresholdDirection.Within && distanceMeters <= trigger.DistanceThresholdMeters ||
            trigger.DistanceThresholdDirection == ProximityThresholdDirection.Outside && distanceMeters > trigger.DistanceThresholdMeters);
        }

        public double KmDistanceTo(Position position)
        {
            return SpeedDatum.CalculateDistanceKM(_position, position);
        }

        public override string ToString()
        {
            return _name + (string.IsNullOrWhiteSpace(_type) ? "" : " (" + _type + ")") + ":  " + Math.Round(_position.Latitude, 4) + "," + Math.Round(_position.Longitude, 4);
        }
    }
}
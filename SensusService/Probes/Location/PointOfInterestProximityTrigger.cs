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

namespace SensusService.Probes.Location
{
    public class PointOfInterestProximityTrigger
    {
        private string _pointOfInterestName;
        private string _pointOfInterestType;
        private double _distanceThresholdMeters;
        private ProximityThresholdDirection _distanceThresholdDirection;

        public string PointOfInterestName
        {
            get 
            {
                return _pointOfInterestName; 
            }
            set
            {
                _pointOfInterestName = value; 
            }
        }

        public string PointOfInterestType
        {
            get
            {
                return _pointOfInterestType;
            }
            set
            {
                _pointOfInterestType = value;
            }
        }

        public double DistanceThresholdMeters
        {
            get
            {
                return _distanceThresholdMeters;
            }
            set
            {
                _distanceThresholdMeters = value;
            }
        }

        public ProximityThresholdDirection DistanceThresholdDirection
        {
            get
            {
                return _distanceThresholdDirection;
            }
            set
            {
                _distanceThresholdDirection = value;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private PointOfInterestProximityTrigger()
        {
        }

        public PointOfInterestProximityTrigger(string pointOfInterestName, string pointOfInterestType, double distanceThresholdMeters, ProximityThresholdDirection distanceThresholdDirection)
            : this()
        {
            if (string.IsNullOrWhiteSpace(pointOfInterestName) && string.IsNullOrWhiteSpace(pointOfInterestType))
                throw new Exception("Points of interest must have a name or type (or both).");
            else if (distanceThresholdMeters <= 0)
                throw new Exception("Invalid distance threshold. Must be greater than zero.");
            
            _pointOfInterestName = pointOfInterestName;
            _pointOfInterestType = pointOfInterestType;
            _distanceThresholdMeters = distanceThresholdMeters;
            _distanceThresholdDirection = distanceThresholdDirection;
        }

        public override string ToString()
        {
            return _pointOfInterestName + " (" + _pointOfInterestType + ")";
        }
    }
}
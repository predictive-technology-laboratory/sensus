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
    public class PointOfInterestProximityDatum : Datum
    {
        private PointOfInterest _pointOfInterest;
        private double _distanceMeters;
        private ProximityThresholdDirection _direction;

        public PointOfInterest PointOfInterest
        {
            get
            {
                return _pointOfInterest;
            }
            set
            {
                _pointOfInterest = value;
            }
        }

        public double DistanceMeters
        {
            get
            {
                return _distanceMeters;
            }
            set
            {
                _distanceMeters = value;
            }
        }

        public ProximityThresholdDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return _pointOfInterest + " (" + _distanceMeters + " meters)";;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private PointOfInterestProximityDatum() 
        {
        }

        public PointOfInterestProximityDatum(DateTimeOffset timestamp, PointOfInterest pointOfInterest, double distanceMeters, ProximityThresholdDirection direction)
            : base(timestamp)
        {
            _pointOfInterest = pointOfInterest;
            _distanceMeters = distanceMeters;
            _direction = direction;
        }
    }
}
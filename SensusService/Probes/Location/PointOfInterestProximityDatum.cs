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
using Xamarin.Geolocation;
using SensusService.Probes.User.ProbeTriggerProperties;

namespace SensusService.Probes.Location
{
    public class PointOfInterestProximityDatum : Datum
    {
        private string _pointOfInterestName;
        private string _pointOfInterestType;
        private double _pointOfInterestLatitude;
        private double _pointOfInterestLongitude;
        private double _distanceMeters;
        private ProximityThresholdDirection _direction;

        [TextProbeTriggerProperty]
        public string Name
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

        [TextProbeTriggerProperty]
        public string Type
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

        [NumberProbeTriggerProperty]
        public double PointOfInterestLatitude
        {
            get
            {
                return _pointOfInterestLatitude;
            }
            set
            {
                _pointOfInterestLatitude = value;
            }
        }

        [NumberProbeTriggerProperty]
        public double PointOfInterestLongitude
        {
            get
            {
                return _pointOfInterestLongitude;
            }
            set
            {
                _pointOfInterestLongitude = value;
            }
        }

        [NumberProbeTriggerProperty]
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
                return Math.Round(_distanceMeters) + "m from " + _pointOfInterestName + (string.IsNullOrWhiteSpace(_pointOfInterestType) ? "" : " (" + _pointOfInterestType + ")");
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
            _pointOfInterestName = pointOfInterest.Name;
            _pointOfInterestType = pointOfInterest.Type;
            _pointOfInterestLatitude = pointOfInterest.Position.Latitude;
            _pointOfInterestLongitude = pointOfInterest.Position.Longitude;
            _distanceMeters = distanceMeters;
            _direction = direction;
        }
    }
}
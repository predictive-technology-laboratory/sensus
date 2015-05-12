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
using SensusService.Probes.Movement;

namespace SensusService.Probes.Location
{
    public class PointOfInterest
    {
        private string _name;
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

        public PointOfInterest(string name, Position position)
        {
            _name = name;
            _position = position;
        }

        public double KmDistanceTo(Position position)
        {
            return SpeedDatum.CalculateDistanceKM(_position, position);
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
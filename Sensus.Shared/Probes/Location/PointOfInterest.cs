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

using System;
using Sensus.Probes.Movement;
using Plugin.Geolocator.Abstractions;

namespace Sensus.Probes.Location
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

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

namespace Sensus.Probes.Location
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
            {
                throw new Exception("No POI was supplied.");
            }
            else if (distanceThresholdMeters <= 0)
            {
                throw new Exception("Invalid distance threshold. Must be greater than zero.");
            }
            else if (distanceThresholdMeters < GpsReceiver.Get().MinimumDistanceThreshold)
            {
                throw new Exception("Distance threshold must be at least " + GpsReceiver.Get().MinimumDistanceThreshold + ".");
            }
            
            _pointOfInterestName = pointOfInterestName;
            _pointOfInterestType = pointOfInterestType;
            _distanceThresholdMeters = distanceThresholdMeters;
            _distanceThresholdDirection = distanceThresholdDirection;
        }

        public override string ToString()
        {
            return _distanceThresholdDirection + " " + _distanceThresholdMeters + "m of " + _pointOfInterestName + (string.IsNullOrWhiteSpace(_pointOfInterestType) ? "" : " (" + _pointOfInterestType + ")");
        }
    }
}

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
using SensusService.Probes.User.ProbeTriggerProperties;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Location
{
    public class PointOfInterestProximityDatum : Datum
    {
        private string _poiName;
        private string _poiType;
        private double _poiLatitude;
        private double _poiLongitude;
        private double _distanceToPoiMeters;
        private double _triggerDistanceMeters;
        private ProximityThresholdDirection _triggerDistanceDirection;

        [Anonymizable("POI Name:", typeof(StringHashAnonymizer), false)]
        [TextProbeTriggerProperty("POI Name")]
        public string PoiName
        {
            get
            {
                return _poiName;
            }
            set
            {
                _poiName = value;
            }
        }

        [Anonymizable("POI Type:", typeof(StringHashAnonymizer), false)]
        [TextProbeTriggerProperty("POI Type")]
        public string PoiType
        {
            get
            {
                return _poiType;
            }
            set
            {
                _poiType = value;
            }
        }

        [Anonymizable("POI Latitude:", new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
        [NumberProbeTriggerProperty("POI Latitude")]
        public double PoiLatitude
        {
            get
            {
                return _poiLatitude;
            }
            set
            {
                _poiLatitude = value;
            }
        }

        [Anonymizable("POI Longitude:", new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
        [NumberProbeTriggerProperty("POI Longitude")]
        public double PoiLongitude
        {
            get
            {
                return _poiLongitude;
            }
            set
            {
                _poiLongitude = value;
            }
        }

        [Anonymizable("Distance (Meters):", new Type[] { typeof(DoubleRoundingTensAnonymizer), typeof(DoubleRoundingHundredsAnonymizer)}, -1)]
        [NumberProbeTriggerProperty("Distance (Meters)")]
        public double DistanceToPoiMeters
        {
            get
            {
                return _distanceToPoiMeters;
            }
            set
            {
                _distanceToPoiMeters = value;
            }
        }

        public double TriggerDistanceMeters
        {
            get
            {
                return _triggerDistanceMeters;
            }
            set
            {
                _triggerDistanceMeters = value;
            }
        }

        public ProximityThresholdDirection TriggerDistanceDirection
        {
            get
            {
                return _triggerDistanceDirection;
            }
            set
            {
                _triggerDistanceDirection = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return Math.Round(_distanceToPoiMeters) + "m from " + _poiName + (string.IsNullOrWhiteSpace(_poiType) ? "" : " (" + _poiType + ")");
            }
        }        

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private PointOfInterestProximityDatum() 
        {
        }

        public PointOfInterestProximityDatum(DateTimeOffset timestamp, PointOfInterest pointOfInterest, double distanceMeters, PointOfInterestProximityTrigger trigger)
            : base(timestamp)
        {
            _poiName = pointOfInterest.Name;
            _poiType = pointOfInterest.Type;
            _poiLatitude = pointOfInterest.Position.Latitude;
            _poiLongitude = pointOfInterest.Position.Longitude;
            _distanceToPoiMeters = distanceMeters;
            _triggerDistanceMeters = trigger.DistanceThresholdMeters;
            _triggerDistanceDirection = trigger.DistanceThresholdDirection;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Name:  " + _poiName + Environment.NewLine +
            "Type:  " + _poiType + Environment.NewLine +
            "Lat:  " + _poiLatitude + Environment.NewLine +
            "Lon:  " + _poiLongitude + Environment.NewLine +
            "Distance:  " + _distanceToPoiMeters + Environment.NewLine +
            "Trigger Distance:  " + _triggerDistanceMeters + Environment.NewLine +
            "Trigger Direction:  " + _triggerDistanceDirection;
        }
    }
}
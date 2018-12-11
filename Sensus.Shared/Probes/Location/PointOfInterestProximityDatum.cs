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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Location
{
    public class PointOfInterestProximityDatum : Datum, IPointOfInterestProximityDatum
    {
        private string _poiName;
        private string _poiType;
        private double _poiLatitude;
        private double _poiLongitude;
        private double _distanceToPoiMeters;
        private double _triggerDistanceMeters;
        private ProximityThresholdDirection _triggerDistanceDirection;

        [Anonymizable("POI Name:", typeof(StringHashAnonymizer), false)]
        [StringProbeTriggerProperty("POI Name")]
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
        [StringProbeTriggerProperty("POI Type")]
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

        [Anonymizable("POI Latitude:", new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("POI Latitude")]
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

        [Anonymizable("POI Longitude:", new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("POI Longitude")]
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

        [Anonymizable("Distance (Meters):", new Type[] { typeof(DoubleRoundingTensAnonymizer), typeof(DoubleRoundingHundredsAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("Distance (Meters)")]
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

        [JsonConverter(typeof(StringEnumConverter))]
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
        /// Gets the string placeholder value, which is the POI name.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _poiName;
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

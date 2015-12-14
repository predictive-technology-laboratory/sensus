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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.ProbeTriggerProperties;
using System.Collections;
using System.Linq;

namespace SensusService.Probes.User
{
    public class ScriptDatum : Datum
    {
        private string _scriptId;
        private string _groupId;
        private string _inputId;
        private object _response;
        private string _triggerDatumId;
        private double? _latitude;
        private double? _longitude;
        private DateTimeOffset _presentationTimestamp;
        private DateTimeOffset? _locationTimestamp;

        public string ScriptId
        {
            get
            {
                return _scriptId;
            }
            set
            {
                _scriptId = value;
            }
        }

        public string GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                _groupId = value;
            }
        }

        public string InputId
        {
            get
            {
                return _inputId;
            }
            set
            {
                _inputId = value;
            }
        }

        public object Response
        {
            get { return _response; }
            set { _response = value; }
        }

        [Anonymizable("Triggering Datum ID:", typeof(StringHashAnonymizer), false)]
        public string TriggerDatumId
        {
            get { return _triggerDatumId; }
            set { _triggerDatumId = value; }
        }

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
        public double? Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
        public double? Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

        public DateTimeOffset PresentationTimestamp
        {
            get
            {
                return _presentationTimestamp;
            }
            set
            {
                _presentationTimestamp = value;
            }
        }

        public DateTimeOffset? LocationTimestamp
        {
            get
            {
                return _locationTimestamp;
            }
            set
            {
                _locationTimestamp = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                if (_response == null)
                    return "No response.";
                else
                {
                    if (_response is IEnumerable)
                    {
                        int numItems = 0;
                        foreach(object item in _response as IEnumerable)
                            ++numItems;
                        
                        return numItems + " responses.";
                    }
                    else
                        return _response.ToString();
                }
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ScriptDatum()
        {
        }

        public ScriptDatum(DateTimeOffset timestamp, string scriptId, string groupId, string inputId, object response, string triggerDatumId, double? latitude, double? longitude, DateTimeOffset presentationTimestamp, DateTimeOffset? locationTimestamp)
            : base(timestamp)
        {
            _scriptId = scriptId;
            _groupId = groupId;
            _inputId = inputId;
            _response = response;
            _triggerDatumId = triggerDatumId == null ? "" : triggerDatumId;
            _latitude = latitude;
            _longitude = longitude;
            _presentationTimestamp = presentationTimestamp;
            _locationTimestamp = locationTimestamp;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Script:  " + _scriptId + Environment.NewLine +
            "Group:  " + _groupId + Environment.NewLine +
            "Input:  " + _inputId + Environment.NewLine +
            "Response:  " + _response + Environment.NewLine +
            "Latitude:  " + _latitude + Environment.NewLine +
            "Longitude:  " + _longitude + Environment.NewLine +
            "Presentation Timestamp:  " + _presentationTimestamp + Environment.NewLine +
            "Location Timestamp:  " + _locationTimestamp;
        }
    }
}
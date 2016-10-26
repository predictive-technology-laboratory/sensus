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
using Newtonsoft.Json;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;

namespace Sensus
{
    /// <summary>
    /// A single unit of sensed information returned by a probe.
    /// </summary>
    public abstract class Datum
    {
        /// <summary>
        /// Settings for serializing Datum objects
        /// </summary>
        private static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Datum FromJSON(string json)
        {
            // null datum properties in the json come from a few places:  a reference-type property that is 
            // null (e.g., a string), a nullable property that has no value, and a property to which a 
            // value-omitting anonymizer is applied. the first two cases will not cause issues when 
            // deserializing. the final case will be problematic if the property is a value-type since we 
            // cannot assign null to a value type. so we're going to ignore null values when deserializing
            // data. in the first two cases above, the property will have the appropriate null value. in the 
            // third case the property will have its default value. be aware that value-type datum type properties
            // with value-omitting anonymizers can thus have their values changed when serialized and then 
            // deserialized (from the original value to their default value). this will usually be okay because
            // any subsequent serialization will omit the value again.
            JSON_SERIALIZER_SETTINGS.NullValueHandling = NullValueHandling.Ignore;

            Datum datum = null;

            try
            {
                datum = JsonConvert.DeserializeObject<Datum>(json, JSON_SERIALIZER_SETTINGS);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to convert JSON to datum:  " + ex.Message, LoggingLevel.Normal, typeof(Datum));
            }

            return datum;
        }

        private string _id;
        private string _deviceId;
        private DateTimeOffset _timestamp;
        private string _protocolId;
        private int _hashCode;
        private bool _anonymized;

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _hashCode = _id.GetHashCode();
            }
        }

        [Anonymizable("Device ID:", typeof(StringHashAnonymizer), false)]
        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }

        [Anonymizable(null, typeof(DateTimeOffsetTimelineAnonymizer), false)]
        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        public string ProtocolId
        {
            get
            {
                return _protocolId;
            }
            set
            {
                _protocolId = value;
            }
        }

        public bool Anonymized
        {
            get
            {
                return _anonymized;
            }
            set
            {
                _anonymized = value;
            }
        }

        [JsonIgnore]
        public abstract string DisplayDetail { get; }

        /// <summary>
        /// Parameterless constructor For JSON.NET deserialization.
        /// </summary>
        protected Datum()
        {
        }

        protected Datum(DateTimeOffset timestamp)
        {
            _timestamp = timestamp;
            _deviceId = SensusServiceHelper.Get().DeviceId;
            _anonymized = false;
            Id = Guid.NewGuid().ToString();
        }

        public string GetJSON(AnonymizedJsonContractResolver anonymizationContractResolver, bool indented)
        {
            JSON_SERIALIZER_SETTINGS.ContractResolver = anonymizationContractResolver;

            // datum objects can be anonymized by omitting values. this is accomplished by setting them to null. also, some fields in datum 
            // objects might have unreliably obtained values (e.g., GPS location might fail and be null). if we ignore fields with null
            // values when serializing, the resulting JSON and other derived structures (e.g., data frames in R) will have differing columns. 
            // so we should include null values to ensure that all JSON values are always included.
            JSON_SERIALIZER_SETTINGS.NullValueHandling = NullValueHandling.Include;

            string json = JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None, JSON_SERIALIZER_SETTINGS);

            // if the json should not be indented, replace all newlines with white space
            if (!indented)
                json = json.Replace('\n', ' ').Replace('\r', ' ');

            return json;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is Datum && (obj as Datum)._id == _id;
        }

        public override string ToString()
        {
            return "Type:  " + GetType().Name + Environment.NewLine +
            "Device ID:  " + _deviceId + Environment.NewLine +
            "Timestamp:  " + _timestamp;
        }
    }
}

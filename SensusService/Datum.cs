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
using Newtonsoft.Json.Serialization;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService
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
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore  // datum objects can be anonymized by omitting values. this is accomplished by setting them to null, which means they aren't even rendered into JSON given this option.
        };

        public static Datum FromJSON(string json)
        {
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

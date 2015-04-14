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
        private static readonly JsonSerializerSettings _serializationSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Datum FromJSON(string json)
        {
            Datum datum = null;

            try { datum = JsonConvert.DeserializeObject<Datum>(json, _serializationSettings); }
            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to convert JSON to datum:  " + ex.Message, LoggingLevel.Normal, typeof(Datum)); }

            return datum;
        }

        private string _id;
        private string _deviceId;
        private DateTimeOffset _timestamp;
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

        [Anonymizable("Device ID", typeof(StringMD5Anonymizer), true)]
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
            _deviceId = SensusServiceHelper.Get().DeviceId;
            _anonymized = false;
            Id = Guid.NewGuid().ToString();
        }

        protected Datum(DateTimeOffset timestamp)
            : this()
        {
            _timestamp = timestamp;
        }

        public string GetJSON(AnonymizedJsonContractResolver anonymizationContractResolver)
        {
            _serializationSettings.ContractResolver = anonymizationContractResolver;
                       
            return JsonConvert.SerializeObject(this, Formatting.None, _serializationSettings).Replace('\n', ' ').Replace('\r', ' ');
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

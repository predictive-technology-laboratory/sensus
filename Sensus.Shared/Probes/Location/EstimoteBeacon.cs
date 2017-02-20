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
using System.Linq;
using EstimoteSdk;
using Newtonsoft.Json;

namespace Sensus.Probes.Location
{
    public class EstimoteBeacon
    {
        public static EstimoteBeacon FromString(string value)
        {
            try
            {
                string[] parts = value.Split(new char[] { ':' }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();

                if (parts.Length != 4)
                {
                    throw new Exception("Invalid beacon:  " + value);
                }

                string name = string.IsNullOrWhiteSpace(parts[0]) ? null : parts[0];

                string uuid = parts[1];

                int? major = null;
                int majorInt;
                if (int.TryParse(parts[2], out majorInt))
                {
                    major = majorInt;
                }

                int? minor = null;
                int minorInt;
                if (int.TryParse(parts[3], out minorInt))
                {
                    minor = minorInt;
                }

                return new EstimoteBeacon(name, uuid, major, minor);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while parsing beacon:  " + ex, LoggingLevel.Normal, typeof(EstimoteBeacon));
                return null;
            }
        }

        public string Name { get; set; }
        public string UUID { get; set; }
        public int? Major { get; set; }
        public int? Minor { get; set; }

        [JsonIgnore]
        public Region Region
        {
            get
            {
                if (UUID == null && Major == null && Minor == null)
                {
                    return new Region(Name, UUID);
                }
                else if (UUID != null && Major == null && Minor == null)
                {
                    return new Region(Name, UUID);
                }
                else if (UUID != null && Major != null && Minor == null)
                {
                    return new Region(Name, UUID, Major.Value);
                }
                else if (UUID != null && Major != null && Minor != null)
                {
                    return new Region(Name, UUID, Major.Value, Minor.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        public EstimoteBeacon(string name, string uuid, int? major, int? minor)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            bool valid = uuid == null && major == null && minor == null ||
                         uuid != null && major == null && minor == null ||
                         uuid != null && major != null && minor == null ||
                         uuid != null && major != null && minor != null;

            if (!valid)
            {
                throw new ArgumentException("Invalid beacon specification");
            }

            Name = name;
            UUID = uuid;
            Major = major;
            Minor = minor;
        }

        public override string ToString()
        {
            return Name + ":" + UUID + ":" + Major + ":" + Minor;
        }
    }
}
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

                string identifier = string.IsNullOrWhiteSpace(parts[0]) ? Guid.NewGuid().ToString() : parts[0];  // identifier cannot be null

                string proximityUUID = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];

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

                return new EstimoteBeacon(identifier, proximityUUID, major, minor);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string Identifier { get; set; }
        public string ProximityUUID { get; set; }
        public int? Major { get; set; }
        public int? Minor { get; set; }

        [JsonIgnore]
        public Region Region
        {
            get
            {
                
#if __ANDROID__
                Java.Util.UUID proximityUUID = ProximityUUID == null ? null : Java.Util.UUID.FromString(ProximityUUID);
                Java.Lang.Integer major = Major == null ? null : Java.Lang.Integer.ValueOf(Major.Value);
                Java.Lang.Integer minor = Minor == null ? null : Java.Lang.Integer.ValueOf(Minor.Value);
#endif

                return new Region(Identifier, proximityUUID, major, minor);
            }
        }

        public EstimoteBeacon(string identifier, string proximityUUID, int? major, int? minor)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Cannot be null or white space.", nameof(identifier));
            }

            bool valid = proximityUUID == null && major == null && minor == null ||
                         proximityUUID != null && major == null && minor == null ||
                         proximityUUID != null && major != null && minor == null ||
                         proximityUUID != null && major != null && minor != null;

            if (!valid)
            {
                throw new ArgumentException("Invalid beacon specification");
            }

            Identifier = identifier;
            ProximityUUID = proximityUUID;
            Major = major;
            Minor = minor;
        }

        public override string ToString()
        {
            return Identifier + ":" + ProximityUUID + ":" + Major + ":" + Minor;
        }
    }
}
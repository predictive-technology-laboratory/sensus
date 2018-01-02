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

#if __ANDROID__
using EstimoteSdk.Observation.Utils;
#elif __IOS__
using Sensus.iOS.Probes.Location;
#endif

namespace Sensus.Probes.Location
{
    public class EstimoteBeacon
    {
        public static EstimoteBeacon FromString(string value)
        {
            try
            {
                string[] parts = value.Split(new char[] { ':' }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();

                if (parts.Length != 2)
                {
                    throw new Exception("Invalid beacon:  " + value);
                }

                string identifier = parts[0];
                string proximityString = parts[1].ToLower();

                Proximity proximity;

                if (proximityString == Proximity.Immediate.ToString().ToLower())
                {
                    proximity = Proximity.Immediate;
                }
                else if (proximityString == Proximity.Near.ToString().ToLower())
                {
                    proximity = Proximity.Near;
                }
                else if (proximityString == Proximity.Far.ToString().ToLower())
                {
                    proximity = Proximity.Far;
                }
                else
                {
                    return null;
                }

                return new EstimoteBeacon(identifier, proximity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string Identifier { get; set; }
        public Proximity ProximityCondition { get; set; }

        public EstimoteBeacon(string identifier, Proximity proximity)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Cannot be null or white space.", nameof(identifier));
            }

            Identifier = identifier;
            ProximityCondition = proximity;
        }

        public bool ProximityConditionSatisfiedBy(Proximity proximity)
        {
            return ProximityCondition == Proximity.Immediate && proximity == Proximity.Immediate ||
                   ProximityCondition == Proximity.Near && (proximity == Proximity.Immediate || proximity == Proximity.Near) ||
                   ProximityCondition == Proximity.Far && (proximity == Proximity.Immediate || proximity == Proximity.Near || proximity == Proximity.Far);
        }

        public override string ToString()
        {
            return Identifier + ":" + ProximityCondition;
        }

        public override bool Equals(object obj)
        {
            EstimoteBeacon beacon = obj as EstimoteBeacon;

            return beacon != null && Identifier == beacon.Identifier;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}
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

namespace Sensus.Probes.Location
{
    public class EstimoteBeacon
    {
        public static EstimoteBeacon FromString(string value)
        {
            try
            {
                string[] parts = value.Split(new char[] { ':' }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();

                return new EstimoteBeacon(parts[0].Trim(), double.Parse(parts[1]));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string Name { get; set; }
        public double ProximityMeters { get; set; }

        public EstimoteBeacon(string name, double proximityMeters)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Cannot be null or white space.", nameof(name));
            }

            Name = name;
            ProximityMeters = proximityMeters;
        }

        public override string ToString()
        {
            return Name + ":" + ProximityMeters;
        }
    }
}
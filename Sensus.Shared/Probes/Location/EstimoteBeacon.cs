﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

namespace Sensus.Probes.Location
{
    public class EstimoteBeacon
    {
        public string Name { get; set; }
        public double ProximityMeters { get; set; }
        public string EventName { get; set; }

        public EstimoteBeacon(string name, double proximityMeters, string eventName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Cannot be null or white space.", nameof(name));
            }

            name = name.Trim();

            if (eventName == null)
            {
                eventName = name;
            }

            Name = name;
            ProximityMeters = proximityMeters;
            EventName = eventName;
        }

        public override string ToString()
        {
            return "Beacon \"" + Name + "\" @ " + Math.Round(ProximityMeters, 1) + " meters raises event \"" + EventName + "\"";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EstimoteBeacon))
            {
                return false;
            }

            EstimoteBeacon beacon = obj as EstimoteBeacon;

            return Name == beacon.Name && Math.Abs(ProximityMeters - beacon.ProximityMeters) < 0.000001 && EventName == beacon.EventName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
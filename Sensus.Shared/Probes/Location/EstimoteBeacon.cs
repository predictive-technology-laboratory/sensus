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

namespace Sensus.Probes.Location
{
    public class EstimoteBeacon
    {
        public string Tag { get; set; }
        public double ProximityMeters { get; set; }
        public string EventName { get; set; }

        public EstimoteBeacon(string tag, double proximityMeters, string eventName)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Cannot be null or white space.", nameof(tag));
            }

            tag = tag.Trim();

            if (eventName == null)
            {
                eventName = tag;
            }

            Tag = tag;
            ProximityMeters = proximityMeters;
            EventName = eventName;
        }

        public override string ToString()
        {
            return "Beacon \"" + Tag + "\" @ " + Math.Round(ProximityMeters, 1) + " meters raises event \"" + EventName + "\"";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EstimoteBeacon))
            {
                return false;
            }

            EstimoteBeacon beacon = obj as EstimoteBeacon;

            return Tag == beacon.Tag && Math.Abs(ProximityMeters - beacon.ProximityMeters) < 0.000001 && EventName == beacon.EventName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

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
using System.Collections.Generic;
using System.Text;
using Sensus.Extensions;

namespace Sensus.Anonymization.Anonymizers
{
    public class GPSAnonymizer : Anonymizer
    {
        bool _useUserBase;
        bool _useLatitude;
        public GPSAnonymizer(bool useUserBase = true, bool useLatitude = true)
        {
            _useUserBase = useUserBase;
            _useLatitude = useLatitude;
        }

        public override string DisplayText
        {
            get
            {
                return "Anonymous Gps";
            }
        }

        public override object Apply(object value, Protocol protocol)
        {
            double realGps = (double)value;
            (float? latitude, float? longitude) baseGps = _useUserBase ? protocol.GpsUserAnonymizerZeroLocationCoordinates : protocol.GpsProtocolAnonymizerZeroLocationCoordinates;
            float? basePart = _useLatitude ? baseGps.latitude : baseGps.longitude;
            double min = _useLatitude ? -180  : - 90;
            double max = _useLatitude ? 180 : 90;
            return FixRange(realGps + basePart, min, max);
        }

        static double? FixRange(double? item, double min, double max)
        {
            if (item.HasValue)
            {
                if (item.Value < min)
                {
                    item = max - Math.Abs(item.Value - min);
                }
                else if (item.Value > max)
                {
                    item = min + Math.Abs(item.Value - max);
                }
            }
            return item;
        }
    }
}

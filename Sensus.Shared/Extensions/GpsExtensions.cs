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
using System.Linq;

namespace Sensus.Extensions
{
    public static class GpsExtensions
    {
        public static (float? latitude, float? longitude) ToLatLong(this string str)
        {
            float o;
            float? latitude = null, longitude = null;
            if (string.IsNullOrWhiteSpace(str) == false)
            {
                if (str.Contains(","))
                {
                    var split = str.Split(',').ToList()
                                        .Where(w => string.IsNullOrWhiteSpace(w) == false && float.TryParse(w.Trim(), out o))
                                        .Select(s => float.Parse(s.Trim())).ToList();
                    latitude = split.Count > 0 ? split[0] : (float?)null;
                    longitude = split.Count() > 1 ? split[1] : (float?)null;
                }
                else if (float.TryParse(str.Trim(), out o))
                {
                    latitude = o;
                }
            }
            return (latitude, longitude);
        }
    }
}

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

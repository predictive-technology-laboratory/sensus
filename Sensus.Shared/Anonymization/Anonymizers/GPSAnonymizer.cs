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

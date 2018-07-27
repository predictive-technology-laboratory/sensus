using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Anonymization.Anonymizers
{
    public class GPSProtocolLongitudeAnonymizer : GPSAnonymizer
    {

        public override string DisplayText
        {
            get
            {
                return "Gps Protocol Anonymizer";
            }
        }

        public GPSProtocolLongitudeAnonymizer()
            : base(false, false)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Anonymization.Anonymizers
{
    public class GPSProtocolLatitudeAnonymizer : GPSAnonymizer
    {

        public override string DisplayText
        {
            get
            {
                return "Gps Protocol Anonymizer";
            }
        }

        public GPSProtocolLatitudeAnonymizer()
            : base(false, true)
        {
        }
    }
}

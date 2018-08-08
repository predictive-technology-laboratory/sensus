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

namespace Sensus.Anonymization.Anonymizers
{
    public abstract class GpsAnonymizer : Anonymizer
    {
        private static double? FixRange(double value, double min, double max)
        {
            if (value < min)
            {
                value = max - Math.Abs(value - min);
            }
            else if (value > max)
            {
                value = min + Math.Abs(value - max);
            }

            return value;
        }

        private GpsAnonymizationMode _anonymizationMode;
        private GpsAnonymizationField _anonymizationField;

        public GpsAnonymizer(GpsAnonymizationMode anonymizationMode, GpsAnonymizationField anonymizationField)
        {
            _anonymizationMode = anonymizationMode;
            _anonymizationField = anonymizationField;
        }

        public override object Apply(object value, Protocol protocol)
        {
            double actualValue = (double)value;

            double originValue, min, max;
            Tuple<double, double> origin = _anonymizationMode == GpsAnonymizationMode.Participant ? protocol.GpsAnonymizationUserOrigin : protocol.GpsAnonymizationProtocolOrigin;
            if (_anonymizationField == GpsAnonymizationField.Latitude)
            {
                originValue = origin.Item1;
                min = -90;
                max = 90;
            }
            else
            {
                originValue = origin.Item2;
                min = -180;
                max = 180;
            }

            return FixRange(actualValue - originValue, min, max);
        }
    }
}
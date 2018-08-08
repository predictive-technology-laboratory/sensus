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
    public abstract class GpAnonymizer : Anonymizer
    {
        private static double? FixRange(double? item, double min, double max)
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

        private GpsAnonymizationMode _anonymizationMode;
        private GpsAnonymizationField _anonymizationField;

        public GpAnonymizer(GpsAnonymizationMode anonymizationMode, GpsAnonymizationField anonymizationField)
        {
            _anonymizationMode = anonymizationMode;
            _anonymizationField = anonymizationField;
        }

        public override object Apply(object value, Protocol protocol)
        {
            double actualValue = (double)value;

            Tuple<double, double> origin = _anonymizationMode == GpsAnonymizationMode.User ? protocol.GpsAnonymizationUserOrigin : protocol.GpsAnonymizationProtocolOrigin;
            double basePart = _anonymizationField == GpsAnonymizationField.Latitude ? origin.Item1 : origin.Item2;
            double min = _anonymizationField == GpsAnonymizationField.Latitude ? -90 : -180;
            double max = _anonymizationField == GpsAnonymizationField.Latitude ? 90 : 180;
            return FixRange(actualValue + basePart, min, max);
        }
    }
}

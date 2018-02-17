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
    /// <summary>
    /// Anonymizes a date/time value by anchoring it to an arbitrary point in the past. The
    /// random anchor time is chosen from the first 1000 years AD, and the anonymized date/time
    /// values are calculated as intervals of time since the random anchor. Thus, the anonymized
    /// date/time values are only meaningful with respect to each other. Their absolute values
    /// will have no meaningful interpretation.
    /// </summary>
    public class DateTimeOffsetTimelineAnonymizer : Anonymizer
    {
        public override string DisplayText
        {
            get
            {
                return "Anonymous Timeline";
            }
        }

        public override object Apply(object value, Protocol protocol)
        {
            DateTimeOffset dateTimeOffsetValue = (DateTimeOffset)value;


            // if the value passed in precedes the random time anchor, the result of the subtraction 
            // will not be representable and will throw an exception. there probably isn't a good case 
            // for anonymizing dates within the first 1000 years AD, so the user has probably misconfigured 
            // the protocol; however, we must return a value, so default to the minimum.

            if (dateTimeOffsetValue >= protocol.RandomTimeAnchor)
            {
                return DateTimeOffset.MinValue + (dateTimeOffsetValue - protocol.RandomTimeAnchor);
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Attempted to anonymize a value that preceded the random anchor time.", LoggingLevel.Normal, GetType());
                return DateTimeOffset.MinValue;
            }
        }
    }
}    
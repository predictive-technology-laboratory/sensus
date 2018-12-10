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

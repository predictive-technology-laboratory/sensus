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
    /// Rounds numeric values to various levels of precision.
    /// </summary>
    public abstract class DoubleRoundingAnonymizer : Anonymizer
    {
        private int _places;

        public override string DisplayText
        {
            get
            {
                return "Round:  " + _places + " places";
            }
        }

        protected DoubleRoundingAnonymizer(int places)
        {
            _places = places;
        }

        public override object Apply(object value, Protocol protocol)
        {
            double doubleValue = (double)value;

            if (_places >= 0)
            {
                return Math.Round(doubleValue, _places);
            }
            else
            {
                // round number to nearest 10^(-_places). for example, -1 would round to tens place, -2 to hundreds, etc.
                doubleValue = doubleValue * Math.Pow(10d, (double)_places);  // shift target place into one position (remember, _places is negative)
                doubleValue = Math.Round(doubleValue, 0);                    // round off fractional part
                return doubleValue / Math.Pow(10d, (double)_places);         // shift target place back to its proper location (remember, _places is negative)
            }
        }
    }
}

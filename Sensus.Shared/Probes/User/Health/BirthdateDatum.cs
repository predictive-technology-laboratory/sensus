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

namespace Sensus.Probes.User.Health
{
    public class BirthdateDatum : Datum
    {
        private DateTimeOffset _birthdate;

        public DateTimeOffset Birthdate
        {
            get
            {
                return _birthdate;
            }
            set
            {
                _birthdate = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Birthdate:  " + _birthdate;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the birth date.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _birthdate;
            }
        }

        public BirthdateDatum(DateTimeOffset timestamp, DateTimeOffset birthdate)
            : base(timestamp)
        {
            _birthdate = birthdate;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Birth date:  " + _birthdate;
        }
    }
}

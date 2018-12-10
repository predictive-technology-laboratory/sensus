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

using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;

namespace Sensus.Probes.Device
{
    public class ScreenDatum : Datum, IScreenDatum
    {
        private bool _on;

        [BooleanProbeTriggerProperty]
        public bool On
        {
            get { return _on; }
            set { _on = value; }
        }

        public override string DisplayDetail
        {
            get { return _on ? "On" : "Off"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is whether the screen is on/off.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _on;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ScreenDatum() { }

        public ScreenDatum(DateTimeOffset timestamp, bool on)
            : base(timestamp)
        {
            _on = on;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "On:  " + _on;
        }
    }
}

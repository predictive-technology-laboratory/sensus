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
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Context
{
    public class BluetoothDeviceProximityDatum : Datum, IBluetoothDeviceProximityDatum
    {
        private string _encounteredDeviceId;

        [StringProbeTriggerProperty]
        [Anonymizable("Encountered Device ID:", typeof(StringHashAnonymizer), false)]
        public string EncounteredDeviceId
        {
            get { return _encounteredDeviceId; }
            set { _encounteredDeviceId = value; }
        }

        public override string DisplayDetail
        {
            get { return _encounteredDeviceId; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the encountered device ID.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _encounteredDeviceId;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private BluetoothDeviceProximityDatum() { }

        public BluetoothDeviceProximityDatum(DateTimeOffset timestamp, string encounteredDeviceId)
            : base(timestamp)
        {
            _encounteredDeviceId = encounteredDeviceId;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Encountered device ID:  " + _encounteredDeviceId;
        }
    }
}

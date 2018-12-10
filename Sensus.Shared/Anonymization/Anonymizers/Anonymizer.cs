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

using Newtonsoft.Json;

namespace Sensus.Anonymization.Anonymizers
{    
    /// <summary>
    /// In-app anonymization is provided by applying various transformations to facets of the data
    /// that are generated. For example, numeric values can be rounded and textual data can be 
    /// passed through one-way hash functions.
    /// </summary>
    public abstract class Anonymizer
    {
        [JsonIgnore]
        public abstract string DisplayText { get; }

        /// <summary>
        /// Applies this anonymizer the value of a datum property. It is important to always return the same datatype as is passed in.
        /// </summary>
        /// <param name="value">Datum property value to anonymize.</param>
        /// <param name="protocol">Protocol that owns the datum being anonymized.</param>
        public abstract object Apply(object value, Protocol protocol);

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            
            // compare most-derived types of current and passed object -- this comparison is used in the UI when the user is selecting the 
            // anonymizer to apply to a datum property.
            return GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}


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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sensus.Probes.User.Health
{
    public class FitzpatrickSkinTypeDatum : Datum
    {
        private FitzpatrickSkinType _fitzPatrickSkinType;

        [JsonConverter(typeof(StringEnumConverter))]
        public FitzpatrickSkinType FitzpatrickSkinType
        {
            get
            {
                return _fitzPatrickSkinType;
            }
            set
            {
                _fitzPatrickSkinType = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Fitzpatrick Skin Type:  " + _fitzPatrickSkinType;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the fitzpatrick skin type.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _fitzPatrickSkinType;
            }
        }

        public FitzpatrickSkinTypeDatum(DateTimeOffset timestamp, FitzpatrickSkinType fitzpatrickSkinType)
            : base(timestamp)
        {
            _fitzPatrickSkinType = fitzpatrickSkinType;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Fitzpatrick Skin Type:  " + _fitzPatrickSkinType;
        }
    }
}

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
using Sensus.Exceptions;
using System.Collections.Generic;

namespace Sensus.Anonymization.Anonymizers
{
    /// <summary>
    /// Computes a cryptographic, one-way has of a textual string.
    /// </summary>
    public class StringHashAnonymizer : Anonymizer
    {        
        public override string DisplayText
        {
            get
            {
                return "Hash";
            }
        }
       
        public override object Apply(object value, Protocol protocol)
        {
            if (value is string)
            {
                return SensusServiceHelper.Get().GetHash(value as string);
            }
            else if (value is IEnumerable<string>)
            {
                List<string> hashes = new List<string>();

                foreach (string s in (value as IEnumerable<string>))
                {
                    hashes.Add(SensusServiceHelper.Get().GetHash(s));
                }

                return hashes;
            }
            else
            {
                throw SensusException.Report("Attempted to apply string hash anonymizer to a non-string value.");
            }
        }
    }
}


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
using SensusService.Exceptions;
using System.Collections.Generic;

namespace SensusService.Anonymization.Anonymizers
{
    public class StringMD5Anonymizer : Anonymizer
    {        
        public override string DisplayText
        {
            get
            {
                return "MD5 Hash";
            }
        }
       
        public override object Apply(object value, Protocol Protocol)
        {
            if (value == null)
                return null;
            
            if (value is string)
                return SensusServiceHelper.Get().GetMd5Hash(value as string);
            else if (value is IEnumerable<string>)
            {
                List<string> md5Hashes = new List<string>();

                foreach (string s in (value as IEnumerable<string>))
                    md5Hashes.Add(SensusServiceHelper.Get().GetMd5Hash(s));
                  
                return md5Hashes;
            }
            else
                throw new SensusException("Attempted to apply string MD5 anonymizer to a non-string value.");
        }
    }
}


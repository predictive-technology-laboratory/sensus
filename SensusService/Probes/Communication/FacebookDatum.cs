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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService
{
    public class FacebookDatum : Datum
    {
        private string _json;

        [Anonymizable("JSON", typeof(StringMD5Anonymizer), false)]
        public string JSON
        {
            get { return _json; }
            set { _json = value; }
        }

        public override string DisplayDetail
        {
            get
            {
                return _json;
            }
        }

        public FacebookDatum(DateTimeOffset timestamp, string json)
            : base(timestamp)
        {
            _json = json;
        }
    }
}
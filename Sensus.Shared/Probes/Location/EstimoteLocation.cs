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

namespace Sensus.Probes.Location
{
    public class EstimoteLocation
    {
        private string _name;
        private string _identifier;

        public string Name
        {
            get { return _name; }
        }

        public string Identifier
        {
            get { return _identifier; }
        }

        public EstimoteLocation(string name, string identifier)
        {
            _name = name.Trim();
            _identifier = identifier.Trim();

            if (string.IsNullOrWhiteSpace(_name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(_identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
            }
        }
    }
}
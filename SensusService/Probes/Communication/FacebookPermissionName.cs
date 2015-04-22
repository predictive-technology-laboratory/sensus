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

namespace SensusService.Probes.Communication
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class FacebookPermissionName : Attribute
    {
        private string _name;
        private string[] _fields;
        private string[] _edges;

        public string Name
        {
            get { return _name; }
        }

        public string[] Fields
        {
            get { return _fields; }
        }

        public string[] Edges
        {
            get { return _edges; }
        }

        public FacebookPermissionName(string name, string[] edges, string[] fields)
            : base()
        {
            _name = name;
            _edges = edges;
            _fields = fields;
        }

        public FacebookPermissionName(string name, string edge, string[] fields)
            : this(name, new string[] { edge }, fields)
        {
        }

        public FacebookPermissionName(string name, string[] edges, string field)
            : this(name, edges, new string[] { field })
        {
        }

        public FacebookPermissionName(string name, string edge, string field)
            : this(name, new string[] { edge }, new string[] { field })
        {
        }
    }
}


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

namespace SensusService.Probes.Apps
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class FacebookPermission : Attribute
    {
        private string _name;
        private string _edge;
        private string[] _fields;

        public string Name
        {
            get { return _name; }
        }

        public string Edge
        {
            get { return _edge; }
        }

        public string[] Fields
        {
            get { return _fields; }
        }            

        public FacebookPermission(string name, string edge, string[] fields)
            : base()
        {
            _name = name;
            _edge = edge;
            _fields = fields;
        }

        public FacebookPermission(string name, string edge, string field)
            : this(name, 
                   edge,
                   field == null ? new string[0] : new string[] { field })
        {
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
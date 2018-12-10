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

namespace Sensus.Probes.Apps
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class FacebookPermission : Attribute
    {
        private string _name;
        private string _edge;
        private string _field;

        public string Name
        {
            get { return _name; }
        }

        public string Edge
        {
            get { return _edge; }
        }

        public string Field
        {
            get { return _field; }
        }            

        public FacebookPermission(string name, string edge, string field)
            : base()
        {
            _name = name;
            _edge = edge;
            _field = field;

            if (_edge == null && _field == null)
            {
                throw SensusException.Report("Facebook permission edge and field are both null for permission \"" + _name + "\".");
            }
        }

        public override string ToString()
        {
            return _name;
        }
    }
}

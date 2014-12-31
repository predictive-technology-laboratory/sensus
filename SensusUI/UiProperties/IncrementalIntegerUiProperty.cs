#region copyright
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
#endregion
 
namespace SensusUI.UiProperties
{
    public abstract class IncrementalIntegerUiProperty : UiProperty
    {
        private int _minimum;
        private int _maximum;
        private int _increment;

        public int Minimum
        {
            get { return _minimum; }
        }

        public int Maximum
        {
            get { return _maximum; }
        }

        public int Increment
        {
            get { return _increment; }
        }

        public IncrementalIntegerUiProperty(int minimum, int maximum, int increment, string labelText, bool editable, int order)
            : base(labelText, editable, order)
        {
            _minimum = minimum;
            _maximum = maximum;
            _increment = increment;
        }
    }
}

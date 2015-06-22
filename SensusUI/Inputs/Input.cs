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
using Xamarin.Forms;
using SensusService.Exceptions;

namespace SensusUI.Inputs
{
    public abstract class Input
    {
        private string _label;
        private View _view;

        public string Label
        {
            get { return _label; }
        }

        public View View 
        { 
            get
            {
                if (_view == null)
                    throw new SensusException("View not set for \"" + GetType().FullName + "\".");
                
                return _view; 
            }
            set
            {
                _view = value;
            }
        }

        public Input(string label)
        {
            _label = label;
        }
    }
}
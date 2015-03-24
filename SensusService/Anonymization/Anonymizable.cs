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
using System.Collections.Generic;
using SensusService.Anonymization.Anonymizers;
using SensusService.Exceptions;

namespace SensusService.Anonymization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Anonymizable : Attribute
    {
        private string _propertyDisplayName;
        private List<Anonymizer> _anonymizers;

        public string PropertyDisplayName
        {
            get { return _propertyDisplayName; }
        }

        public List<Anonymizer> Anonymizers
        {
            get { return _anonymizers; }
        }

        public Anonymizable(string propertyDisplayName, Type[] anonymizerTypes)
        {
            _propertyDisplayName = propertyDisplayName;

            _anonymizers = new List<Anonymizer>();
            foreach (Type anonymizerType in anonymizerTypes)
            {
                Anonymizer anonymizer = Activator.CreateInstance(anonymizerType) as Anonymizer;
                if (anonymizer == null)
                    throw new SensusException("Attempted to create an anonymizer that does not derive from Anonymizer.");
                
                _anonymizers.Add(anonymizer);
            }
        }
    }
}  
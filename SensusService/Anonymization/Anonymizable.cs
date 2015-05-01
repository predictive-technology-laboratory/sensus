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
        private List<Anonymizer> _availableAnonymizers;
        private Anonymizer _defaultAnonymizer;

        public string PropertyDisplayName
        {
            get { return _propertyDisplayName; }
        }

        public List<Anonymizer> AvailableAnonymizers
        {
            get { return _availableAnonymizers; }
        }            

        public Anonymizer DefaultAnonymizer
        {
            get { return _defaultAnonymizer; }
        }

        public Anonymizable(string propertyDisplayName, Type[] availableAnonymizerTypes, int defaultAnonymizerIndex)
        {                       
            _propertyDisplayName = propertyDisplayName;

            // instantiate available anonymizers
            _availableAnonymizers = new List<Anonymizer>();
            _availableAnonymizers.Add(new ValueOmittingAnonymizer());
            foreach (Type availableAnonymizerType in availableAnonymizerTypes)
            {
                Anonymizer availableAnonymizer = Activator.CreateInstance(availableAnonymizerType) as Anonymizer;

                if (availableAnonymizer == null)
                    throw new SensusException("Attempted to create an anonymizer from a type that does not derive from Anonymizer.");
                
                _availableAnonymizers.Add(availableAnonymizer);
            }

            if (defaultAnonymizerIndex < -1 || defaultAnonymizerIndex >= _availableAnonymizers.Count)
                throw new SensusException("Attempted to set default anonymizer for property outside the bounds of available types:  " + defaultAnonymizerIndex);

            // set default anonymizer if requested
            if (defaultAnonymizerIndex >= 0)
                _defaultAnonymizer = _availableAnonymizers[defaultAnonymizerIndex];
        }

        public Anonymizable(string propertyDisplayName, Type anonymizerType, bool anonymizeByDefault)
            : this(propertyDisplayName, anonymizerType == null ? new Type[0] : new Type[] { anonymizerType }, anonymizeByDefault ? 0 : -1)
        {
        }
    }
}  
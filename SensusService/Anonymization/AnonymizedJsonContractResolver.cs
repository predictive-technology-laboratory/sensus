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
using Newtonsoft.Json.Serialization;
using System.Reflection;
using SensusService.Anonymization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SensusService.Exceptions;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Anonymization
{
    public class AnonymizedJsonContractResolver : DefaultContractResolver
    {
        private class AnonymizedValueProvider : IValueProvider
        {
            private PropertyInfo _property;
            private Anonymizer _anonymizer;
            private Protocol _protocol;

            public AnonymizedValueProvider(PropertyInfo property, Anonymizer anonymizer, Protocol protocol)
            {
                _property = property;
                _anonymizer = anonymizer;
                _protocol = protocol;
            }

            public void SetValue(object target, object value)
            {
                _property.SetValue(target, value);
            }

            public object GetValue(object target)
            {
                // TODO:  Does this work for timestamps?

                Datum datum = target as Datum;

                if (datum == null)
                    throw new SensusException("Attempted to apply anonymizer to non-datum object.");

                object propertyValue = _property.GetValue(datum);

                // don't re-anonymize data
                if (datum.Anonymized)
                    return propertyValue;
                else
                    return _anonymizer.Apply(_property.GetValue(target), _protocol);
            }
        }

        private Protocol _protocol;
        private Dictionary<PropertyInfo, Anonymizer> _propertyAnonymizer;

        public Protocol Protocol
        {
            get
            {
                return _protocol;
            }
            set
            {
                _protocol = value;
            }
        }

        /// <summary>
        /// Allows JSON serialization of the _propertyAnonymizer collection, which includes unserializable
        /// PropertyInfo objects.
        /// </summary>
        /// <value>The property string anonymizer string.</value>
        public ObservableCollection<string> PropertyStringAnonymizerString
        {
            get
            {
                // get specs for current collection -- this is what will be serialized
                ObservableCollection<string> propertyAnonymizerSpecs = new ObservableCollection<string>();
                foreach (PropertyInfo property in _propertyAnonymizer.Keys)
                    propertyAnonymizerSpecs.Add(property.ReflectedType.FullName + "-" + property.Name + ":" + _propertyAnonymizer[property].GetType().FullName);

                // if we're deserializing, then propertyAnonymizerSpecs will be empty here but will shortly be filled -- handle the filling events.
                propertyAnonymizerSpecs.CollectionChanged += (o, a) =>
                {
                    foreach (string propertyAnonymizerSpec in a.NewItems)
                    {
                        string[] propertyAnonymizerParts = propertyAnonymizerSpec.Split(':');

                        string[] propertyParts = propertyAnonymizerParts[0].Split('-');
                        Type datumType = Type.GetType(propertyParts[0]);
                        PropertyInfo property = datumType.GetProperty(propertyParts[1]);

                        Type anonymizerType = Type.GetType(propertyAnonymizerParts[1]);
                        Anonymizer anonymizer = Activator.CreateInstance(anonymizerType) as Anonymizer;

                        _propertyAnonymizer.Add(property, anonymizer);
                    }
                };

                return propertyAnonymizerSpecs;
            }                
        }

        private AnonymizedJsonContractResolver()
        {
            _propertyAnonymizer = new Dictionary<PropertyInfo, Anonymizer>();
        }

        public AnonymizedJsonContractResolver(Protocol protocol)
            : this()
        {
            _protocol = protocol;
        }

        public Anonymizer GetAnonymizer(PropertyInfo property)
        {
            Anonymizer anonymizer;

            _propertyAnonymizer.TryGetValue(property, out anonymizer);

            return anonymizer;
        }

        public void SetAnonymizer(PropertyInfo property, Anonymizer anonymizer)
        {
            _propertyAnonymizer[property] = anonymizer;
        }

        public void ClearAnonymizer(PropertyInfo property)
        {
            if (_propertyAnonymizer.ContainsKey(property))
                _propertyAnonymizer.Remove(property);
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            Anonymizer anonymizer;
            if (property != null && _propertyAnonymizer.TryGetValue(property, out anonymizer))
                return new AnonymizedValueProvider(property, _propertyAnonymizer[key], _protocol);
            else
                return base.CreateMemberValueProvider(member);
        }
    }
}
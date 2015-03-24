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

namespace SensusService
{
    public class AnonymizedJsonContractResolver : DefaultContractResolver
    {
        private class AnonymizedValueProvider : IValueProvider
        {
            private PropertyInfo _property;
            private Anonymizer _anonymizer;

            public AnonymizedValueProvider(PropertyInfo property, Anonymizer anonymizer)
            {
                _property = property;
                _anonymizer = anonymizer;
            }

            public void SetValue(object target, object value)
            {
                _property.SetValue(target, value);
            }

            public object GetValue(object target)
            {
                return _anonymizer.Apply(_property.GetValue(target));
            }
        }

        private Dictionary<PropertyInfo, Anonymizer> _propertyAnonymizer;

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
                    propertyAnonymizerSpecs.Add(property.DeclaringType.FullName + "-" + property.Name + ":" + _propertyAnonymizer[property].GetType().FullName);

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

        public AnonymizedJsonContractResolver()
        {
            _propertyAnonymizer = new Dictionary<PropertyInfo, Anonymizer>();
        }

        public Anonymizer GetAnonymizer(PropertyInfo property)
        {
            if (_propertyAnonymizer.ContainsKey(property))
                return _propertyAnonymizer[property];
            else
                return null;
        }

        public void SetAnonymizer(PropertyInfo property, Anonymizer anonymizer)
        {
            _propertyAnonymizer[property] = anonymizer;
        }

        public void ClearAnonymizer(PropertyInfo property)
        {
            _propertyAnonymizer.Remove(property);
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;

            Anonymizer anonymizer;
            if (property != null && _propertyAnonymizer.TryGetValue(property, out anonymizer))
                return new AnonymizedValueProvider(property, anonymizer);
            else
                return base.CreateMemberValueProvider(member);
        }
    }
}
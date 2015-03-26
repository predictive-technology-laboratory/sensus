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
        private class AnonymizedMemberValueProvider : IValueProvider
        {
            private PropertyInfo _property;
            private AnonymizedJsonContractResolver _contractResolver;
            private IValueProvider _defaultMemberValueProvider;
            
            public AnonymizedMemberValueProvider(PropertyInfo property, IValueProvider defaultMemberValueProvider, AnonymizedJsonContractResolver contractResolver)
            {                
                _property = property;
                _defaultMemberValueProvider = defaultMemberValueProvider;
                _contractResolver = contractResolver;
            }

            public void SetValue(object target, object value)
            {
                _defaultMemberValueProvider.SetValue(target, value);
            }

            public object GetValue(object target)
            {
                Datum datum = target as Datum;

                if (datum == null)
                    throw new SensusException("Attempted to apply serialize/anonymize non-datum object.");

                // if we're processing the Anonymized property, return true so that the output JSON properly reflects the fact that the datum has been passed through an anonymizer.
                if (_property.DeclaringType == typeof(Datum) && _property.Name == "Anonymized")
                    return true;
                else
                {
                    object propertyValue = _defaultMemberValueProvider.GetValue(datum);

                    // TODO:  Make sure default anonymizers get used (even with no UI interaction.

                    // don't re-anonymize data, and don't anonymize values for which we have no anonymizer.
                    Anonymizer anonymizer;
                    if (datum.Anonymized || !_contractResolver._propertyAnonymizer.TryGetValue(datum.GetType().GetProperty(_property.Name), out anonymizer))  // we re-get the PropertyInfo from the datum's type so that it matches our dictionary of PropertyInfo objects (the reflected type needs to be the most-derived, which doesn't happen leading up to this point for some reason).
                        return propertyValue;
                    // anonymize!
                    else
                        return anonymizer.Apply(propertyValue, _contractResolver.Protocol);
                }
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
        /// PropertyInfo objects. Store the type/name of the PropertyInfo objects, along with the type of 
        /// anonymizer.
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

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
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
            _propertyAnonymizer.Remove(property);
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (!(member is PropertyInfo))
                throw new SensusException("Attempted to serialize/anonymize non-property datum member.");

            return new AnonymizedMemberValueProvider(member as PropertyInfo, base.CreateMemberValueProvider(member), this);
        }
    }
}
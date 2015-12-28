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
using Newtonsoft.Json;
using System.Linq;

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
                if (target == null)
                    throw new SensusException("Attempted to process a null object.");
                else if (target is Datum)
                {
                    Datum datum = target as Datum;

                    // if we're processing the Anonymized property, return true so that the output JSON properly reflects the fact that the datum has been passed through an anonymizer (this regardless of whether anonymization of data was actually performed)
                    if (_property.DeclaringType == typeof(Datum) && _property.Name == "Anonymized")
                        return true;
                    else
                    {
                        object propertyValue = _defaultMemberValueProvider.GetValue(datum);

                        // don't re-anonymize property values, don't anonymize when we don't have an anonymizer, and don't attempt anonymization if the property value is null
                        Anonymizer anonymizer;
                        if (datum.Anonymized || (anonymizer = _contractResolver.GetAnonymizer(datum.GetType().GetProperty(_property.Name))) == null || propertyValue == null)  // we re-get the PropertyInfo from the datum's type so that it matches our dictionary of PropertyInfo objects (the reflected type needs to be the most-derived, which doesn't happen leading up to this point for some reason).
                            return propertyValue;
                        // anonymize!
                        else
                            return anonymizer.Apply(propertyValue, _contractResolver.Protocol);
                    }
                }
                // if the target is not a datum object, simply return the member value (e.g., for input completion records).
                else
                    return _defaultMemberValueProvider.GetValue(target);
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
                lock (_propertyAnonymizer)
                {
                    // get specs for current collection. in the case of serialization, this will store the current _propertyAnonymizer details. in 
                    // the case of deserialization, this will be empty and will be filled in.
                    ObservableCollection<string> propertyAnonymizerSpecs = new ObservableCollection<string>();
                    foreach (PropertyInfo property in _propertyAnonymizer.Keys)
                    {                    
                        string anonymizerTypeStr = "";
                        Anonymizer anonymizer = _propertyAnonymizer[property];
                        if (anonymizer != null)
                            anonymizerTypeStr = anonymizer.GetType().FullName;
                    
                        propertyAnonymizerSpecs.Add(property.ReflectedType.FullName + "-" + property.Name + ":" + anonymizerTypeStr);  // use the reflected type and not the declaring type, because we want different anonymizers for the same base-class property (e.g., DeviceId) within child-class implementations.
                    }

                    // if we're deserializing, then propertyAnonymizerSpecs will be filled up after it is returned. handle the addition of 
                    // items to rebuild the _propertyAnonymizer collection.
                    propertyAnonymizerSpecs.CollectionChanged += (o, a) =>
                    {
                        foreach (string propertyAnonymizerSpec in a.NewItems)
                        {
                            string[] propertyAnonymizerParts = propertyAnonymizerSpec.Split(new char[]{ ':' }, StringSplitOptions.RemoveEmptyEntries);

                            string[] propertyParts = propertyAnonymizerParts[0].Split('-');
                            Type datumType = Type.GetType(propertyParts[0]);
                            PropertyInfo property = datumType.GetProperty(propertyParts[1]);

                            Anonymizer anonymizer = null;
                            if (propertyAnonymizerParts.Length > 1)
                            {
                                Type anonymizerType = Type.GetType(propertyAnonymizerParts[1]);
                                anonymizer = Activator.CreateInstance(anonymizerType) as Anonymizer;
                            }

                            _propertyAnonymizer.Add(property, anonymizer);
                        }
                    };

                    return propertyAnonymizerSpecs;
                }
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

        public void SetAnonymizer(PropertyInfo property, Anonymizer anonymizer)
        {
            lock (_propertyAnonymizer)
            {
                _propertyAnonymizer[property] = anonymizer;
            }
        }

        public Anonymizer GetAnonymizer(PropertyInfo property)
        {
            lock (_propertyAnonymizer)
            {
                Anonymizer anonymizer;

                _propertyAnonymizer.TryGetValue(property, out anonymizer);

                return anonymizer;
            }
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // for some reason, even JsonIgnore'd properties like the DisplayDetail are serialized if the datum class
            // inherits from ImpreciseDatum. to cover this, simply ignore get-only properties.
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            return properties.Where(p => p.Writable).ToList();
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (!(member is PropertyInfo))
                throw new SensusException("Attempted to serialize/anonymize non-property datum member.");

            return new AnonymizedMemberValueProvider(member as PropertyInfo, base.CreateMemberValueProvider(member), this);
        }
    }
}
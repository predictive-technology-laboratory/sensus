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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Linq;
using Sensus.Anonymization.Anonymizers;
using Sensus.Exceptions;

namespace Sensus.Anonymization
{
    /// <summary>
    /// Applies declared anonymizers to property values when serializing data to JSON.
    /// </summary>
    public class AnonymizedJsonContractResolver : DefaultContractResolver
    {
        private class AnonymizedPropertyValueProvider : IValueProvider
        {
            private PropertyInfo _property;
            private IValueProvider _defaultMemberValueProvider;
            private AnonymizedJsonContractResolver _contractResolver;

            public AnonymizedPropertyValueProvider(PropertyInfo property, IValueProvider defaultMemberValueProvider, AnonymizedJsonContractResolver contractResolver)
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
                {
                    throw SensusException.Report("Attempted to process a null object.");
                }
                // if the target object is a Datum, consider anonymizing the property value
                else if (target is Datum)
                {
                    Datum datum = target as Datum;

                    // if we're processing the Anonymized property, return true so that the output JSON properly reflects the fact that the datum has 
                    // been passed through an anonymizer (this regardless of whether an anonymization transformation was actually applied). this also
                    // ensures that, if the JSON is deserialized and then reserialized, we won't attempt to anonymize the JSON again (see checks below).
                    if (_property.DeclaringType == typeof(Datum) && _property.Name == nameof(Datum.Anonymized))
                    {
                        return true;
                    }
                    else
                    {
                        // first get the property's value in the default way
                        object propertyValue = _defaultMemberValueProvider.GetValue(datum);

                        Anonymizer anonymizer;
                        if (propertyValue == null ||                                                                              // don't attempt anonymization if the property value is null
                            datum.Anonymized ||                                                                                   // don't re-anonymize property values
                            (anonymizer = _contractResolver.GetAnonymizer(datum.GetType().GetProperty(_property.Name))) == null)  // don't anonymize when we don't have an anonymizer. we re-get the PropertyInfo from the datum's type so that it matches our dictionary of PropertyInfo objects (the reflected type needs to be the most-derived, which doesn't happen leading up to this point for some reason).
                        {
                            return propertyValue;
                        }
                        else
                        {
                            return anonymizer.Apply(propertyValue, _contractResolver.Protocol);
                        }
                    }
                }
                // if the target is not a datum object (e.g., for InputCompletionRecords stored in ScriptDatum objects), simply return the member value in the default way.
                else
                {
                    return _defaultMemberValueProvider.GetValue(target);
                }
            }
        }

        private Protocol _protocol;
        private Dictionary<PropertyInfo, Anonymizer> _propertyAnonymizer;

		public ObservableCollection<(string, string, Anonymizer)> PropertyAnonymizers
		{
			get
			{
				IEnumerable<(string, string, Anonymizer)> anonymizers = _propertyAnonymizer
					.Where(x => x.Value != null)
					.Select(x => (x.Key.DeclaringType.FullName, x.Key.Name, x.Value));

				ObservableCollection<(string, string, Anonymizer)> observableAnonymizers = new ObservableCollection<(string, string, Anonymizer)>(anonymizers);

				observableAnonymizers.CollectionChanged += (o, e) =>
				{
					foreach ((string Type, string Property, Anonymizer Anonymizer) item in e.NewItems)
					{
						_propertyAnonymizer.Add(Type.GetType(item.Type).GetProperty(item.Property), item.Anonymizer);
					}
				};

				return observableAnonymizers;
			}
		}

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
        //public ObservableCollection<string> PropertyStringAnonymizerString
        //{
        //    get
        //    {
        //        lock (_propertyAnonymizer)
        //        {
        //            // get specs for current collection. in the case of serialization, this will store the current _propertyAnonymizer details. in 
        //            // the case of deserialization, this will be empty and will be filled in.
        //            ObservableCollection<string> propertyAnonymizerSpecs = new ObservableCollection<string>();
        //            foreach (PropertyInfo property in _propertyAnonymizer.Keys)
        //            {
        //                string anonymizerTypeStr = "";
        //                Anonymizer anonymizer = _propertyAnonymizer[property];
        //                if (anonymizer != null)
        //                {
        //                    anonymizerTypeStr = anonymizer.GetType().FullName;
        //                }

        //                propertyAnonymizerSpecs.Add(property.ReflectedType.FullName + "-" + property.Name + ":" + anonymizerTypeStr);  // use the reflected type and not the declaring type, because we want different anonymizers for the same base-class property (e.g., DeviceId) within child-class implementations.
        //            }

        //            // if we're deserializing, then propertyAnonymizerSpecs will be filled up after it is returned. handle the addition of 
        //            // items to rebuild the _propertyAnonymizer collection.
        //            propertyAnonymizerSpecs.CollectionChanged += (o, a) =>
        //            {
        //                foreach (string propertyAnonymizerSpec in a.NewItems)
        //                {
        //                    string[] propertyAnonymizerParts = propertyAnonymizerSpec.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

        //                    string[] propertyParts = propertyAnonymizerParts[0].Split('-');
        //                    Type datumType = Type.GetType(propertyParts[0]);
        //                    PropertyInfo property = datumType.GetProperty(propertyParts[1]);

        //                    Anonymizer anonymizer = null;
        //                    if (propertyAnonymizerParts.Length > 1)
        //                    {
        //                        Type anonymizerType = Type.GetType(propertyAnonymizerParts[1]);
        //                        anonymizer = Activator.CreateInstance(anonymizerType) as Anonymizer;
        //                    }

        //                    _propertyAnonymizer.Add(property, anonymizer);
        //                }
        //            };

        //            return propertyAnonymizerSpecs;
        //        }
        //    }
        //}

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

        /// <summary>
        /// Gets a list of properties of a type that should be serialized. The purpose of this class is to
        /// anonymize Datum objects and their child classes; however, if a Datum (or child) class contains
        /// serializable properties involving other classes, those types will also be processed with this
        /// method as well as CreateMemberValueProvider. So, don't assume that the properties will always
        /// come from Datum-based classes.
        /// </summary>
        /// <returns>The properties.</returns>
        /// <param name="type">Type.</param>
        /// <param name="memberSerialization">Member serialization.</param>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // for some reason, even JsonIgnore'd properties like the DisplayDetail are serialized if the datum class
            // inherits from ImpreciseDatum. to help cover this, simply ignore get-only properties.
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            return properties.Where(p => p.Writable).ToList();
        }

        /// <summary>
        /// Creates a value provider for a member. Only works for PropertyInfo members and will throw an
        /// exception for all other members.
        /// </summary>
        /// <returns>The member value provider.</returns>
        /// <param name="member">Member.</param>
        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            IValueProvider defaultValueProvider = base.CreateMemberValueProvider(member);

            // only datum objects should be serialized, and these should only contain serialized properties.
            PropertyInfo propertyInfo = member as PropertyInfo;
            if (propertyInfo == null)
            {
                // this is unexpected. report the issue and return the default serializer.
                SensusException.Report("Attempted to serialize/anonymize non-property datum member:  " + member);
                return defaultValueProvider;
            }
            else
            {
                return new AnonymizedPropertyValueProvider(propertyInfo, defaultValueProvider, this);
            }
        }
    }
}
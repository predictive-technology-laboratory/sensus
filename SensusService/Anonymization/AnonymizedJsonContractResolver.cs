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
        public class AnonymizedValueProvider : IValueProvider
        {
            private PropertyInfo _property;
            private DatumPropertyAnonymizer _anonymizer;

            public AnonymizedValueProvider(PropertyInfo property, DatumPropertyAnonymizer anonymizer)
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

        private Dictionary<PropertyInfo, DatumPropertyAnonymizer> _propertyAnonymizer;

        public ObservableCollection<string> PropertyStringAnonymizerString
        {
            get
            {
                ObservableCollection<string> propStrAnonStr = new ObservableCollection<string>();
                foreach (PropertyInfo property in _propertyAnonymizer.Keys)
                    propStrAnonStr.Add(property.DeclaringType.FullName + "-" + property.Name + ":" + _propertyAnonymizer[property].GetType().FullName);

                propStrAnonStr.CollectionChanged += (o, a) =>
                {
                    foreach (string s in a.NewItems)
                    {
                        string[] parts = s.Split(':');

                        string[] propertyParts = parts[0].Split('-');
                        Type propertyType = Type.GetType(propertyParts[0]);
                        PropertyInfo property = propertyType.GetProperty(propertyParts[1]);

                        Type anonymizerType = Type.GetType(parts[1]);
                        DatumPropertyAnonymizer anonymizer = Activator.CreateInstance(anonymizerType) as DatumPropertyAnonymizer;

                        _propertyAnonymizer.Add(property, anonymizer);
                    }
                };

                return propStrAnonStr;
            }                
        }

        public AnonymizedJsonContractResolver()
        {
            _propertyAnonymizer = new Dictionary<PropertyInfo, DatumPropertyAnonymizer>();
        }

        public DatumPropertyAnonymizer GetAnonymizer(PropertyInfo property)
        {
            if (_propertyAnonymizer.ContainsKey(property))
                return _propertyAnonymizer[property];
            else
                return null;
        }

        public void SetAnonymizer(PropertyInfo property, DatumPropertyAnonymizer anonymizer)
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

            DatumPropertyAnonymizer anonymizer;
            if (property != null && _propertyAnonymizer.TryGetValue(property, out anonymizer))
                return new AnonymizedValueProvider(property, anonymizer);
            else
                return base.CreateMemberValueProvider(member);
        }
    }
}
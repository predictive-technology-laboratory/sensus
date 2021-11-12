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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Sensus
{
	public class JsonIgnoreContractResolver : DefaultContractResolver
	{
		private readonly HashSet<string> _ignoredPropertyNames;
		private readonly HashSet<Type> _ignoredTypes;
		private readonly HashSet<MemberInfo> _ignoredMembers;
		public JsonIgnoreContractResolver()
		{
			_ignoredPropertyNames = new HashSet<string>();
			_ignoredTypes = new HashSet<Type>();
			_ignoredMembers = new HashSet<MemberInfo>();
		}

		public JsonIgnoreContractResolver(params string[] ignoredProperties)
		{
			_ignoredPropertyNames = new HashSet<string>(ignoredProperties ?? new string[0]);
		}

		public JsonIgnoreContractResolver(params Type[] ignoredTypes)
		{
			_ignoredTypes = new HashSet<Type>(ignoredTypes ?? new Type[0]);
		}

		public JsonIgnoreContractResolver(params MemberInfo[] ignoredMembers)
		{
			_ignoredMembers = new HashSet<MemberInfo>(ignoredMembers ?? new MemberInfo[0]);
		}

		public void Ignore(string name)
		{
			_ignoredPropertyNames.Add(name);
		}
		public void Ignore(Type type)
		{
			_ignoredTypes.Add(type);
		}
		public void Ignore<TType>()
		{
			Ignore(typeof(TType));
		}
		public void Ignore(MemberInfo member)
		{
			_ignoredMembers.Add(member);
		}
		public void Ignore<TObject, TProperty>(Expression<Func<TObject, TProperty>> member)
		{
			MemberExpression memberExpression = member.Body as MemberExpression;

			_ignoredMembers.Add(memberExpression.Member);
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			JsonProperty property = base.CreateProperty(member, memberSerialization);

			if (_ignoredPropertyNames.Contains(property.PropertyName) || _ignoredPropertyNames.Contains(member.Name) || _ignoredTypes.Contains(property.PropertyType) || _ignoredMembers.Contains(member))
			{
				property.ShouldSerialize = _ => false;
			}

			return property;
		}
	}
}

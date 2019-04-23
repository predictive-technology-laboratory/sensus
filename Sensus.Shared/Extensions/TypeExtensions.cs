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
using System.Collections.Generic;

namespace Sensus.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the ancestor types of a given type.
        /// </summary>
        /// <returns>The ancestor types.</returns>
        /// <param name="type">Type to get ancestor types for.</param>
        /// <param name="includeObject">If set to <c>true</c> include <see cref="object"/> type.</param>
        public static List<Type> GetAncestorTypes(this Type type, bool includeObject)
        {
            List<Type> ancestorTypes = new List<Type>();

            while (type != null && (includeObject || type != typeof(object)))
            {
                ancestorTypes.Add(type);
                type = type.BaseType;
            }

            return ancestorTypes;
        }

        public static bool ImplementsInterface<DatumInterface>(this Type type) where DatumInterface : IDatum
        {
            return type.IsAssignableFrom(typeof(DatumInterface));
        }
    }
}
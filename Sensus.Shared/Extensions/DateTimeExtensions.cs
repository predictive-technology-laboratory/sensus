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

namespace Sensus.Shared.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime Max(this DateTime d1, DateTime d2)
        {
            return d1 > d2 ? d1 : d2;
        }

        public static DateTime Max(this DateTime? d1, DateTime d2)
        {
            return d1 > d2 ? d1.Value : d2;
        }

        public static DateTime Max(this DateTime d1, DateTime? d2)
        {
            return d2 > d1 ? d2.Value : d1;
        }

        public static DateTime Min(this DateTime d1, DateTime d2)
        {
            return d1 < d2 ? d1 : d2;
        }

        public static DateTime Min(this DateTime? d1, DateTime d2)
        {
            return d1 < d2 ? d1.Value : d2;
        }

        public static DateTime Min(this DateTime d1, DateTime? d2)
        {
            return d2 < d1 ? d2.Value : d1;
        }
    }
}
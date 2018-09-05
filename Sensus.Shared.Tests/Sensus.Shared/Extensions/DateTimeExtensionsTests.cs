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
using Xunit;
using Sensus.Extensions;

namespace Sensus.Tests.Extensions
{
    
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void Max()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxNullableSecond()
        {
            var date1 = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.Equal(date1, date1.Max(date2));
            Assert.Equal(date1, date2.Max(date1));
        }

        [Fact]
        public void Min()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.Equal(date1, date1.Min(date2));
            Assert.Equal(date1, date2.Min(date1));
        }

        [Fact]
        public void MinEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Min(date2));
            Assert.Equal(date2, date2.Min(date1));
        }

        [Fact]
        public void MinNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Min(date2));
            Assert.Equal(date2, date2.Min(date1));
        }

        [Fact]
        public void MinNullableSecond()
        {
            var date1 = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.Equal(date1, date1.Min(date2));
            Assert.Equal(date1, date2.Min(date1));
        }
    }
}
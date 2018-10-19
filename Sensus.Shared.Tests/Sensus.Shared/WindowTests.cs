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

namespace Sensus.Tests
{
    
    public class WindowTests
    {
        [Fact]
        public void PointScheduleTriggerParse()
        {
            var t = new Window("10:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.Equal(TimeSpan.Zero, t.Duration);
        }

        [Fact]
        public void WindowScheduleTriggerParse()
        {
            var t = new Window("10:22-12:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.Equal(TimeSpan.FromHours(2), t.Duration);
        }

        [Fact]
        public void PointScheduleToString()
        {
            var t = new Window("10:22");

            Assert.Equal("10:22", t.ToString());
        }

        [Fact]
        public void WindowScheduleToString()
        {
            var t = new Window("10:22-12:22");

            Assert.Equal("10:22-12:22", t.ToString());
        }

        [Fact]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:23-12:23");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.True(t2.CompareTo(t1) >= 0);
        }

        [Fact]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:22-12:22");

            Assert.Equal(0, t1.CompareTo(t2));
        }

        [Fact]
        public void DowPointScheduleTriggerParse()
        {
            var t = new Window("Su-10:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.Equal(TimeSpan.Zero, t.Duration);
        }

        [Fact]
        public void DowWindowScheduleTriggerParse()
        {
            var t = new Window("Mo-10:22-12:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.Equal(TimeSpan.FromHours(2), t.Duration);
        }

        [Fact]
        public void DowPointScheduleToString()
        {
            var t = new Window("Tu-10:22");

            Assert.Equal("Tu-10:22", t.ToString());
        }

        [Fact]
        public void DowWindowScheduleToString()
        {
            var t = new Window("We-10:22-12:22");

            Assert.Equal("We-10:22-12:22", t.ToString());
        }

        [Fact]
        public void DowWindowScheduleCompareToDifferent()
        {
            var t1 = new Window("Th-10:22-12:22");
            var t2 = new Window("Fr-10:22-12:22");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.True(t2.CompareTo(t1) >= 0);
        }

        [Fact]
        public void DowWindowScheduleCompareToEqual()
        {
            var t1 = new Window("Sa-10:22-12:22");
            var t2 = new Window("Sa-10:22-12:22");

            Assert.Equal(0, t1.CompareTo(t2));
        }
    }
}
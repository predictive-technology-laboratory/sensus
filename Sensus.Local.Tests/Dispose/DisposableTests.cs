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
using NUnit.Framework;
using Sensus.Tools;

namespace Sensus.Local.Tests
{
    [TestFixture]
    public class DisposableTests
    {
        private class BadPattern : Disposable
        {
            protected override void Dispose(bool disposing)
            {
                //this sould call base.Dispose(disposing). Otherwise, other classes in the inheritance hierarchy might not have their resources disposed.
            }
        }

        private class GoodPattern : Disposable
        {
            public new void DisposeCheck()
            {
                base.DisposeCheck();
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    throw new Exception("Dispose called but disposing is false. Something is wrong.");
                }

                base.Dispose(true);
            }
        }

        [Test]
        public void BadPatternThrowsException()
        {
            var badPattern = new BadPattern();

            Assert.Throws<InvalidOperationException>(badPattern.Dispose);
        }

        [Test]
        public void GoodPatternThrowsNoException()
        {
            var goodPattern = new GoodPattern();

            Assert.DoesNotThrow(goodPattern.Dispose);
        }

        [Test]
        public void GoodPatternMultiDisposeThrowsNoException()
        {
            var goodPattern = new GoodPattern();

            Assert.DoesNotThrow(goodPattern.Dispose);
            Assert.DoesNotThrow(goodPattern.Dispose);
        }

        [Test]
        public void GoodPatternDisposeCheckPassesBeforeDispose()
        {
            var goodPattern = new GoodPattern();
            
            Assert.DoesNotThrow(goodPattern.DisposeCheck);
        }

        [Test]
        public void GoodPatternDisposeCheckFailsAfterDispose()
        {
            var goodPattern = new GoodPattern();

            goodPattern.Dispose();

            Assert.Throws<ObjectDisposedException>(goodPattern.DisposeCheck);            
        }
    }
}
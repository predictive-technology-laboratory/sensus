//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Xunit;
using Sensus.Dispose;

namespace Sensus.Tests.Dispose
{
    
    public class DisposableTests
    {
        #region Private Classes
        private class BadPattern : Disposable
        {
            public bool FixDispose { private get; set; }            

            protected override void Dispose(bool disposing)
            {
                //this sould call base.Dispose(disposing). Otherwise, other classes in the inheritance hierarchy might not have their resources disposed.
                if (FixDispose) { base.Dispose(disposing); }
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
        #endregion

        [Fact]
        public void BadPatternThrowsException()
        {
            using (var badPattern = new BadPattern())
            {
                Assert.Throws(typeof(InvalidOperationException), (Action)badPattern.Dispose);

                badPattern.FixDispose = true;
            }
        }

        [Fact]
        public void GoodPatternThrowsNoException()
        {
            using (var goodPattern = new GoodPattern())
            {
                goodPattern.Dispose();
            }
        }

        [Fact]
        public void GoodPatternMultiDisposeThrowsNoException()
        {
            using (var goodPattern = new GoodPattern())
            {
                goodPattern.Dispose();
                goodPattern.Dispose();
            }
        }

        [Fact]
        public void GoodPatternDisposeCheckPassesBeforeDispose()
        {
            using (var goodPattern = new GoodPattern())
            {
                goodPattern.DisposeCheck();
            }
        }

        [Fact]
        public void GoodPatternDisposeCheckFailsAfterDispose()
        {
            using (var goodPattern = new GoodPattern())
            {
                goodPattern.Dispose();

                Assert.Throws(typeof(ObjectDisposedException), (Action)goodPattern.DisposeCheck);
            }
        }
    }
}

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
using Sensus.Callbacks;
using Sensus.Concurrent;
using Sensus.Context;
using Sensus.Encryption;
using Sensus.Notifications;

namespace Sensus.Tests.Classes
{
    public class TestSensusContext : ISensusContext
    {
        public Platform Platform { get; set; }
        public IConcurrent MainThreadSynchronizer { get; set; }
        public IEncryption SymmetricEncryption { get; set; }
        public Notifier Notifier { get; set; }
        public CallbackScheduler CallbackScheduler { get; set; }
        public string ActivationId { get; set; }
        public string IamRegion { get; set; }
        public string IamAccessKey { get; set; }
        public string IamAccessKeySecret { get; set; }
        public PowerConnectionChangeListener PowerConnectionChangeListener { get; set; }

        public TestSensusContext()
        {
            Platform = Platform.Test;
            MainThreadSynchronizer = new LockConcurrent();
            SymmetricEncryption = new SymmetricEncryption("91091462A8D6FD3B4DB1D91C731070F10460D73AEE0377EDC2585C42F70A84A5");
            Notifier = new TestSensusNotifier();
            CallbackScheduler = new TestSensusCallbackScheduler();
            ActivationId = "asdfadsf";
        }
    }
}

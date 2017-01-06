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
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using Sensus.Concurrent;

namespace Sensus.Tests.Concurrent
{
    public abstract class IConcurrentTests
    {
        #region Fields
        private const int DelayTime = 2;
        private readonly IConcurrent _concurrent;
        #endregion

        #region Constructors
        protected IConcurrentTests(IConcurrent concurrent)
        {
            _concurrent = concurrent;
        }
        #endregion

        [Test(Description = "If this fails the DelayTime likely isn't large enough to cause a failure if future tests break.")]
        public void DelayIsLongEnough()
        {
            var test = new List<int> { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }
            });

            var task2 = Task.Run(() =>
            {
                test.Add(4);
                Task.Delay(DelayTime).Wait();
                test.Add(5);
            });

            Assert.Throws<AggregateException>(() => Task.WaitAll(task1, task2));
        }

        [Test]
        public void ExecuteThreadSafeActionThrowsNoException()
        {
            var test = new List<int> { 1, 2, 3 };

            Assert.DoesNotThrow(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    test.Add(4);

                    foreach (var i in test)
                    {
                        Task.Delay(DelayTime).Wait();
                    }

                    test.Add(5);
                });
            });
        }

        [Test]
        public void ExecuteThreadSafeFuncThrowsNoException()
        {
            var test = new List<int> { 1, 2, 3 };

            Assert.DoesNotThrow(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    test.Add(4);

                    foreach (var i in test)
                    {
                        Task.Delay(DelayTime).Wait();
                    }

                    test.Add(5);

                    return test;
                });
            });
        }

        [Test]
        public void ExecuteThreadSafeActionIsThreadSafe()
        {
            var test = new List<int> { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    foreach (var i in test)
                    {
                        Task.Delay(DelayTime).Wait();
                    }
                });
            });

            var task2 = Task.Run(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    test.Add(4);
                    Task.Delay(DelayTime).Wait();
                    test.Add(5);
                });
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void ExecuteThreadSafeFuncIsThreadSafe()
        {
            var test = new List<int> { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                var output = _concurrent.ExecuteThreadSafe(() =>
                {
                    foreach (var i in test)
                    {
                        Task.Delay(DelayTime).Wait();
                    }

                    return test;
                });

                Assert.AreSame(output, test);
            });

            var task2 = Task.Run(() =>
            {
                var output = _concurrent.ExecuteThreadSafe(() =>
                {
                    test.Add(4);
                    Task.Delay(DelayTime).Wait();
                    test.Add(5);

                    return test;
                });

                Assert.AreSame(output, test);
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void ExecuteThreadSafeActionIsSynchronous()
        {
            var test = new List<int> { 1, 2, 3 };            

            _concurrent.ExecuteThreadSafe(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }
            });

            test.Add(4);
            Task.Delay(DelayTime).Wait();
            test.Add(5);

            Assert.Contains(4, test);
            Assert.Contains(5, test);
        }

        [Test]
        public void ExecuteThreadSafeFuncIsSynchronous()
        {
            var test = new List<int> { 1, 2, 3 };

            _concurrent.ExecuteThreadSafe(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }

                return test;
            });

            test.Add(4);
            Task.Delay(DelayTime).Wait();
            test.Add(5);
            
            Assert.Contains(4, test);
            Assert.Contains(5, test);
        }

        [Test]
        public void ExecuteThreadSafeFuncReturnsCorrectly()
        {
            var test = new List<int> { 1, 2, 3 };

            var output = _concurrent.ExecuteThreadSafe(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }

                return test;
            });

            Assert.AreSame(output, test);
        }

        [Test]
        public void InnerExecuteThreadSafeActionNoDeadlock()
        {
            var test = new List<int> { 1, 2, 3 };

            _concurrent.ExecuteThreadSafe(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    for (var i = 4; i <= 6; i++)
                    {
                        test.Add(i);
                        Task.Delay(DelayTime).Wait();
                    }
                });
            });

            Assert.AreEqual(6, test.Count, "It appears that we deadlocked because the action didn't finish adding items");
        }

        [Test]
        public void InnerExecuteThreadSafeFuncNoDeadlock()
        {
            var test = new List<int> { 1, 2, 3 };

            _concurrent.ExecuteThreadSafe(() =>
            {
                return _concurrent.ExecuteThreadSafe(() =>
                {
                    for(var i = 4; i <= 6; i++)
                    {
                        test.Add(i);
                        Task.Delay(DelayTime).Wait();
                    }                        

                    return test;
                });
            });

            //we check test because in the case of a deadlock I'm not sure what output will be returned...
            Assert.AreEqual(6, test.Count, "It appears that we deadlocked because the func didn't finish adding items");
        }
    }
}
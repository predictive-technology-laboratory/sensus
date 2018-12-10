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
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Sensus.Concurrent;

namespace Sensus.Tests.Concurrent
{
    public abstract class ConcurrentTests
    {
        #region Fields
        private const int DelayTime = 2;
        private readonly IConcurrent _concurrent;
        #endregion

        #region Constructors
        protected ConcurrentTests(IConcurrent concurrent)
        {
            _concurrent = concurrent;
        }
        #endregion

        [Fact]
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

        [Fact]
        public void ExecuteThreadSafeActionThrowsNoException()
        {
            var test = new List<int> { 1, 2, 3 };

            _concurrent.ExecuteThreadSafe(() =>
            {
                test.Add(4);

                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }

                test.Add(5);
            });
        }

        [Fact]
        public void ExecuteThreadSafeFuncThrowsNoException()
        {
            var test = new List<int> { 1, 2, 3 };

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
        }

        [Fact]
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

        [Fact]
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

                Assert.Same(output, test);
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

                Assert.Same(output, test);
            });

            Task.WaitAll(task1, task2);
        }

        [Fact]
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

            Assert.True(test.Contains(4));
            Assert.True(test.Contains(5));
        }

        [Fact]
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

            Assert.True(test.Contains(4));
            Assert.True(test.Contains(5));
        }

        [Fact]
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

            Assert.Same(output, test);
        }

        [Fact]
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

            Assert.Equal(6, test.Count);
        }

        [Fact]
        public void InnerExecuteThreadSafeFuncNoDeadlock()
        {
            var test = new List<int> { 1, 2, 3 };

            _concurrent.ExecuteThreadSafe(() =>
            {
                return _concurrent.ExecuteThreadSafe(() =>
                {
                    for (var i = 4; i <= 6; i++)
                    {
                        test.Add(i);
                        Task.Delay(DelayTime).Wait();
                    }

                    return test;
                });
            });

            //we check test because in the case of a deadlock I'm not sure what output will be returned...
            Assert.Equal(6, test.Count);
        }

        [Fact]
        public void ExecuteThreadSafeActionCatchesExceptionFromSameThread()
        {
            Assert.Throws(typeof(Exception), () =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    _concurrent.ExecuteThreadSafe(() =>
                    {
                        throw new Exception();
                    });
                });
            });
        }

        [Fact]
        public void ExecuteThreadSafeFuncCatchesExceptionFromSameThread()
        {
            Assert.Throws(typeof(Exception), () =>
            {
                _concurrent.ExecuteThreadSafe(() =>
                {
                    int x = _concurrent.ExecuteThreadSafe(() =>
                    {
                        throw new Exception();
#pragma warning disable CS0162 // Unreachable code detected
                        return 1;  // required to make this a function rather than an action
#pragma warning restore CS0162 // Unreachable code detected
                    });
                });
            });
        }
    }
}

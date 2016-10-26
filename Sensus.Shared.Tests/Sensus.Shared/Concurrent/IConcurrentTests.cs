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

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Sensus.Concurrent;

namespace Sensus.Tests.Concurrent
{    
    public abstract class IConcurrentTests
    {        
        #region Fields
        private const int DelayTime = 100;
        private readonly IConcurrent _concurrent;
        #endregion

        #region Constructors
        protected IConcurrentTests(IConcurrent concurrent)
        {
            _concurrent = concurrent;
        }
        #endregion

        [Test]
        public void ActionIsThreadSafe()
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
        public void FuncIsThreadSafe()
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
        public void BasicActionIsThreadSafe()
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

            Assert.Contains(4, test);
            Assert.Contains(5, test);
        }

        [Test]
        public void BasicFuncIsThreadSafe()
        {
            var test = new List<int> { 1, 2, 3 };

            var output = _concurrent.ExecuteThreadSafe(() =>
            {
                test.Add(4);

                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }

                test.Add(5);

                return test;
            });

            Assert.Contains(4, output);
            Assert.Contains(5, output);
            Assert.AreSame(test, output);
        }

        [Test]
        public void InnerActionIsThreadSafe()
        {
            var test = new List<int> { 1, 2, 3 };
            var cancel = new System.Threading.CancellationTokenSource();

            var task = Task.Run(() =>
            {
                _concurrent.ExecuteThreadSafe(() =>
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
            }, cancel.Token);

            task.Wait(1000);

            if (!task.IsCompleted)
            {
                cancel.Cancel();
                throw new System.Exception("It appears that we deadlocked");
            }

            Assert.IsTrue(task.IsCompleted);

            Assert.Contains(4, test);
            Assert.Contains(5, test);
        }

        [Test]
        public void InnerFuncIsThreadSafe()
        {
            var test = new List<int> { 1, 2, 3 };
            var output = default(List<int>);
            var cancel = new System.Threading.CancellationTokenSource();

            var task = Task.Run(() =>
            {
                output = _concurrent.ExecuteThreadSafe(() =>
                {
                    return _concurrent.ExecuteThreadSafe(() =>
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
            }, cancel.Token);

            task.Wait(1000);

            if (!task.IsCompleted)
            {
                cancel.Cancel();
                throw new System.Exception("It appears that we deadlocked");
            }

            Assert.Contains(4, output);
            Assert.Contains(5, output);
            Assert.AreSame(test, output);
        }
    }
}
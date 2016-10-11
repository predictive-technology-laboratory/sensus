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

using System.Threading.Tasks;
using NUnit.Framework;
using Sensus.Tools;

namespace Sensus.Tests.Local.Concurrent
{
    [TestFixture]
    public class ConcurrentObservableCollectionTests
    {        
        #region Fields        
        private const int DelayTime = 100;
        private readonly IConcurrent Concurrent;
        #endregion

        public ConcurrentObservableCollectionTests(): this(new LockConcurrent())
        { }

        public ConcurrentObservableCollectionTests(IConcurrent concurrent)
        {
            Concurrent = concurrent;
        }

        [Test]
        public void AddIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int> (Concurrent) { 1, 2, 3 };

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

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void InsertIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int>(Concurrent) { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }
            });

            var task2 = Task.Run(() =>
            {
                test.Insert(0, 4);
                Task.Delay(DelayTime).Wait();
                test.Insert(0, 5);
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void RemoveIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int>(Concurrent) { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }
            });

            var task2 = Task.Run(() =>
            {
                test.Remove(2);
                Task.Delay(DelayTime).Wait();
                test.Remove(1);
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void ClearIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int>(Concurrent) { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                foreach (var i in test)
                {
                    Task.Delay(DelayTime).Wait();
                }
            });

            var task2 = Task.Run(() =>
            {
                Task.Delay(DelayTime).Wait();
                test.Clear();
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void ContainsIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int>(Concurrent) { 1, 2, 3 };

            var task1 = Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    test.Contains(2);
                    Task.Delay(DelayTime).Wait();
                    test.Contains(4);
                }
            });

            var task2 = Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    test.Add(4);
                    Task.Delay(DelayTime).Wait();
                    test.Add(5);
                }
            });

            Task.WaitAll(task1, task2);
        }

        [Test]
        public void CopyToIsThreadSafe()
        {
            var test = new ConcurrentObservableCollection<int>(Concurrent) { 1, 2, 3 };
            var out1 = new int[5];
            var out2 = new int[5];

            var task1 = Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    test.CopyTo(out1, 0);
                    Task.Delay(DelayTime).Wait();
                    test.CopyTo(out2, 0);
                }
            });

            var task2 = Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    test.Add(4);
                    Task.Delay(DelayTime).Wait();
                    test.Remove(4);
                }
            });

            Task.WaitAll(task1, task2);
        }
    }
}
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
    
    public class ConcurrentObservableCollectionTests
    {        
        private const int DelayTime = 2;
        private readonly IConcurrent Concurrent;

        public ConcurrentObservableCollectionTests()
        {
            Concurrent = new LockConcurrent();
        }

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
                test.Insert(0, 4);
                Task.Delay(DelayTime).Wait();
                test.Insert(0, 5);
            });

            Assert.Throws<AggregateException>(() => Task.WaitAll(task1, task2));
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

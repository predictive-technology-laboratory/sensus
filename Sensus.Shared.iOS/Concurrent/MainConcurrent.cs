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

using Foundation;
using System;
using System.Threading;
using Sensus.Shared.Dispose;
using Sensus.Shared.Concurrent;
using Xamarin.Forms;

namespace Sensus.Shared.iOS.Concurrent
{
    public class MainConcurrent : Disposable, IConcurrent
    {
        private readonly int? _waitTime;

        public MainConcurrent(int? waitTime = null)
        {
            _waitTime = waitTime;
        }

        public void ExecuteThreadSafe(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException();
            }

            if (NSThread.IsMain)
            {
                action();
            }
            else
            {
                var runWait = new ManualResetEvent(false);

                NSThread.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        runWait.Set();
                    }
                });

                if (_waitTime == null)
                    runWait.WaitOne();
                else
                    runWait.WaitOne(_waitTime.Value);
            }
        }

        public T ExecuteThreadSafe<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (NSThread.IsMain)
            {
                return func();
            }
            else
            {
                var result = default(T);
                var runWait = new ManualResetEvent(false);

                NSThread.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        result = func();
                    }
                    finally
                    {
                        runWait.Set();
                    }
                });

                if (_waitTime == null)
                    runWait.WaitOne();
                else
                    runWait.WaitOne(_waitTime.Value);

                return result;
            }
        }
    }
}
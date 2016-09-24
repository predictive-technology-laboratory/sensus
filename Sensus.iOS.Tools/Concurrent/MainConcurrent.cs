﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Threading;
using Foundation;
using Sensus.Tools;
using Xamarin.Forms;

namespace Sensus.iOS.Concurrent
{
    public class MainConcurrent: Disposable, IConcurrent
    {
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

            var runWait = new ManualResetEvent(false);

            Device.BeginInvokeOnMainThread(() =>
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

            runWait.WaitOne();
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

            var result = default(T);
            var runWait = new ManualResetEvent(false);

            Device.BeginInvokeOnMainThread(() =>
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

            runWait.WaitOne();
            return result;
        }
    }
}
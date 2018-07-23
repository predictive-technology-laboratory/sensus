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
using Sensus.Dispose;
using Sensus.Concurrent;
using Xamarin.Forms;

namespace Sensus.iOS.Concurrent
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
                throw new ArgumentNullException(nameof(action));
            }

            if (NSThread.IsMain)
            {
                action();
            }
            else
            {
                ManualResetEvent actionWait = new ManualResetEvent(false);
                Exception actionException = null;

                NSThread.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        actionException = ex;
                    }
                    finally
                    {
                        actionWait.Set();
                    }
                });

                if (_waitTime == null)
                {
                    actionWait.WaitOne();
                }
                else
                {
                    actionWait.WaitOne(_waitTime.Value);
                }

                if (actionException != null)
                {
                    throw actionException;
                }
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
                T funcResult = default(T);
                ManualResetEvent funcWait = new ManualResetEvent(false);
                Exception funcException = null;

                NSThread.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        funcResult = func();
                    }
                    catch (Exception ex)
                    {
                        funcException = ex;
                    }
                    finally
                    {
                        funcWait.Set();
                    }
                });

                if (_waitTime == null)
                {
                    funcWait.WaitOne();
                }
                else
                {
                    funcWait.WaitOne(_waitTime.Value);
                }

                if (funcException != null)
                {
                    throw funcException;
                }

                return funcResult;
            }
        }
    }
}
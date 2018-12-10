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
using System.Threading;
using Sensus.Dispose;
using Sensus.Concurrent;
using Android.OS;

namespace Sensus.Android.Concurrent
{
    /// <remarks>
    /// Provides concurrent access to the main thread of the application. Note that <see cref="Xamarin.Forms.Device.BeginInvokeOnMainThread"/> invokes 
    /// off the activity, and Sensus does not always have an activity, so we create a handler bound to the main thread's looper instead.
    /// </remarks>
    public class MainConcurrent : Disposable, IConcurrent
    {
        private readonly int? _waitTime;
        private readonly Handler _handler;

        public MainConcurrent(int? waitTime = null)
        {
            _waitTime = waitTime;
            _handler = new Handler(Looper.MainLooper);
        }

        public void ExecuteThreadSafe(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_handler.Looper.IsCurrentThread)
            {
                action();
            }
            else
            {
                ManualResetEvent actionWait = new ManualResetEvent(false);
                Exception actionException = null;

                _handler.Post(() =>
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

            if (_handler.Looper.IsCurrentThread)
            {
                return func();
            }
            else
            {
                T funcResult = default(T);
                ManualResetEvent funcWait = new ManualResetEvent(false);
                Exception funcException = null;

                _handler.Post(() =>
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handler.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

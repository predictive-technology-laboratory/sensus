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
using Sensus.Tools;
using Android.OS;

namespace Sensus.Android.Tools
{
    /// <remarks>
    /// Device.BeginInvokeOnMainThread invokes off the activity. Sensus does not always have an activity, so we create a handler bound to the main thread's looper instead.
    /// </remarks>
    public class MainConcurrent: Disposable, IConcurrent
    {
        #region Fields
        private readonly Handler _handler;
        #endregion

        #region Constructor
        public MainConcurrent()
        {
            _handler = new Handler(Looper.MainLooper);
        }
        #endregion

        #region Public Methods
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
                var runWait = new ManualResetEvent(false);
                _handler.Post(() =>
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
                var runWait = new ManualResetEvent(false);
                var result  = default(T);

                _handler.Post(() =>
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handler.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}